"""Gọi bộ hiệu chỉnh / đánh giá của backend, lưu kết quả thô ra tệp để đối chiếu.

Cách dùng: python hieu_chinh.py phan-bo | do-nguong-nhung | do-nguong-tfidf | chay
"""
import json
import sys
import time
from pathlib import Path

from chung import cho_api, dang_nhap, goi

THU_MUC = Path(__file__).resolve().parent
DUONG_DAN = {
    "phan-bo": "/api/danh-gia/phan-bo",
    "do-nguong-nhung": "/api/danh-gia/do-nguong?phuongPhap=nhung",
    "do-nguong-tfidf": "/api/danh-gia/do-nguong?phuongPhap=tfidf",
    "chay": "/api/danh-gia/chay",
}


buoc = sys.argv[1] if len(sys.argv) > 1 else "phan-bo"
cho_api()
token = dang_nhap("giamdoc")

bat_dau = time.time()
st, kq = goi("GET", DUONG_DAN[buoc], token)
if st != 200:
    sys.exit(f"HTTP {st}: {str(kq)[:2000]}")

tep = THU_MUC / f"ket_qua_{buoc}.json"
tep.write_text(json.dumps(kq, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"[{buoc}] xong sau {time.time() - bat_dau:.1f}s, lưu ở {tep.name}\n")
print(json.dumps(kq, ensure_ascii=False, indent=1)[:12000])
