"""
Dịch vụ nhúng véc-tơ ngữ nghĩa cho phân hệ gợi ý người thực hiện.

Chỉ làm MỘT việc: nhận văn bản, trả về véc-tơ. Không biết gì về nhiệm vụ, nhân viên hay công
thức chấm điểm — phần đó nằm ở ASP.NET Core. Tách như vậy thì đổi mô hình không phải sửa
nghiệp vụ, và sửa nghiệp vụ không phải đụng tới Python.

Mô hình: intfloat/multilingual-e5-small — đã huấn luyện sẵn, KHÔNG tự huấn luyện, KHÔNG
fine-tune. Chạy bằng CPU. Lần chạy đầu tự tải mô hình (khoảng 470 MB) về thư mục .cache
nằm cạnh tệp này; các lần sau nạp thẳng từ đó, không cần mạng.

Chạy:  .venv\\Scripts\\python -m uvicorn app:app --port 8000
"""
from __future__ import annotations

import os
import threading
import time
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Literal

# Phải đặt TRƯỚC khi nạp transformers: thư viện đọc biến này để biết lưu mô hình ở đâu.
# Để trong thư mục dự án cho dễ tìm và dễ xoá, thay vì rải vào thư mục người dùng.
THU_MUC = Path(__file__).resolve().parent
os.environ.setdefault("HF_HOME", str(THU_MUC / ".cache"))
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")

import torch  # noqa: E402
from fastapi import FastAPI, HTTPException  # noqa: E402
from pydantic import BaseModel, Field  # noqa: E402
from transformers import AutoModel, AutoTokenizer  # noqa: E402

TEN_MO_HINH = os.getenv("AI_MODEL", "intfloat/multilingual-e5-small")

# E5 được huấn luyện với tiền tố phân vai. Thiếu tiền tố thì véc-tơ vẫn sinh ra bình thường
# nhưng bị lệch, điểm tương đồng kém hẳn — một lỗi âm thầm, không báo gì cả. Dịch vụ tự thêm
# tiền tố để phía gọi khỏi phải biết chi tiết riêng của từng mô hình.
#   query   : phía đi tìm   — nội dung nhiệm vụ
#   passage : phía được tìm — hồ sơ nhân viên, mô tả phòng ban, mô tả kỹ năng
TIEN_TO = {"query": "query: ", "passage": "passage: "}

TOI_DA_VAN_BAN = 512   # số văn bản mỗi lần gọi
TOI_DA_KY_TU = 4000    # mỗi văn bản; dài hơn thì mô hình cũng cắt ở 512 token
DO_DAI_TOKEN = 512
KICH_THUOC_LO = 32

_bo_tach_tu = None
_mo_hinh = None

# Bộ tách từ "nhanh" của Hugging Face viết bằng Rust, không an toàn khi nhiều luồng dùng chung
# (lỗi "Already borrowed"). FastAPI chạy endpoint đồng bộ trên nhiều luồng, nên phải khoá lại.
# Lượng gọi thấp, khoá không làm chậm gì đáng kể.
_khoa = threading.Lock()


def _nap_mo_hinh(ten: str):
    # Đã có trong .cache thì nạp thẳng, không hỏi mạng — khởi động nhanh và chạy được khi mất mạng.
    try:
        return (AutoTokenizer.from_pretrained(ten, local_files_only=True),
                AutoModel.from_pretrained(ten, local_files_only=True))
    except OSError:
        return AutoTokenizer.from_pretrained(ten), AutoModel.from_pretrained(ten)


@asynccontextmanager
async def _vong_doi(_app: FastAPI):
    global _bo_tach_tu, _mo_hinh
    bat_dau = time.perf_counter()
    _bo_tach_tu, _mo_hinh = _nap_mo_hinh(TEN_MO_HINH)
    _mo_hinh.eval()
    print(f"Đã nạp {TEN_MO_HINH}: {_mo_hinh.config.hidden_size} chiều, "
          f"mất {time.perf_counter() - bat_dau:.1f} giây", flush=True)
    yield


def _nhung(van_ban: list[str]) -> torch.Tensor:
    """
    Trung bình các véc-tơ token (bỏ phần đệm), rồi chuẩn hoá độ dài về 1 — đúng cách thẻ mô
    hình E5 hướng dẫn.

    Không dùng thư viện sentence-transformers vì nó kéo theo scikit-learn, mà tệp DLL của
    scikit-learn từng bị Smart App Control trên Windows chặn. Đã đối chiếu hai cách trên cùng
    câu đầu vào: lệch tối đa 5×10⁻⁸, tức là trùng khớp.
    """
    cac_lo = []
    for i in range(0, len(van_ban), KICH_THUOC_LO):
        lo = van_ban[i:i + KICH_THUOC_LO]
        dau_vao = _bo_tach_tu(lo, max_length=DO_DAI_TOKEN, padding=True, truncation=True, return_tensors="pt")
        with torch.inference_mode():
            an = _mo_hinh(**dau_vao).last_hidden_state
        mat_na = dau_vao["attention_mask"].unsqueeze(-1).to(an.dtype)
        trung_binh = (an * mat_na).sum(dim=1) / mat_na.sum(dim=1).clamp(min=1e-9)
        cac_lo.append(torch.nn.functional.normalize(trung_binh, p=2, dim=1))
    return torch.cat(cac_lo)


app = FastAPI(title="TaskApp — dịch vụ nhúng ngữ nghĩa", version="1.0", lifespan=_vong_doi)


class YeuCauNhung(BaseModel):
    texts: list[str] = Field(min_length=1, max_length=TOI_DA_VAN_BAN)
    kind: Literal["query", "passage"]


class KetQuaNhung(BaseModel):
    model: str
    dim: int
    vectors: list[list[float]]
    ms: float


@app.get("/health")
def suc_khoe() -> dict:
    return {
        "ready": _mo_hinh is not None,
        "model": TEN_MO_HINH,
        "dim": _mo_hinh.config.hidden_size if _mo_hinh is not None else None,
    }


@app.post("/embed", response_model=KetQuaNhung)
def nhung(yc: YeuCauNhung) -> KetQuaNhung:
    if _mo_hinh is None:
        raise HTTPException(status_code=503, detail="Mô hình chưa nạp xong.")

    van_ban = [TIEN_TO[yc.kind] + t.strip()[:TOI_DA_KY_TU] for t in yc.texts]

    bat_dau = time.perf_counter()
    with _khoa:
        # Véc-tơ dài đúng 1, nên độ tương đồng cosine chỉ còn là tích vô hướng — phía .NET nhân
        # rồi cộng là xong, không phải chia cho độ dài.
        vec = _nhung(van_ban)

    return KetQuaNhung(
        model=TEN_MO_HINH,
        dim=int(vec.shape[1]),
        vectors=[[round(x, 6) for x in hang] for hang in vec.tolist()],
        ms=round((time.perf_counter() - bat_dau) * 1000, 1),
    )
