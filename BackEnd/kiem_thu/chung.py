"""Tiện ích dùng chung cho các script kiểm thử gọi API thật.

Trước đây bốn hàm dưới đây được chép lại nguyên si trong từng script, nên sửa một chỗ là ba chỗ
kia lệch. Gom về đây.

Cấu hình bằng biến môi trường, có giá trị mặc định cho máy phát triển:

    KIEM_THU_GOC       địa chỉ API          (mặc định http://localhost:5099)
    KIEM_THU_MAT_KHAU  mật khẩu tài khoản   (mặc định 123456)
"""
from __future__ import annotations

import json
import os
import sys
import time
import urllib.error
import urllib.request

GOC = os.getenv("KIEM_THU_GOC", "http://localhost:5099").rstrip("/")
MAT_KHAU = os.getenv("KIEM_THU_MAT_KHAU", "123456")

# Một số endpoint đánh giá chạy lại mô hình trên toàn bộ lịch sử nên rất lâu.
CHO_MAC_DINH = 900


def goi(phuong_thuc, duong_dan, token=None, than=None, cho=CHO_MAC_DINH):
    """Gọi API, trả về (mã HTTP, thân đã giải mã JSON hoặc None).

    Không ném lỗi khi HTTP 4xx/5xx — bộ kiểm thử cần kiểm cả mã lỗi, nên lỗi phải là giá trị
    trả về chứ không phải ngoại lệ.
    """
    du_lieu = json.dumps(than).encode("utf-8") if than is not None else None
    yc = urllib.request.Request(GOC + duong_dan, data=du_lieu, method=phuong_thuc)
    yc.add_header("Content-Type", "application/json")
    if token:
        yc.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(yc, timeout=cho) as r:
            noi_dung = r.read().decode("utf-8")
            return r.status, (json.loads(noi_dung) if noi_dung else None)
    except urllib.error.HTTPError as e:
        noi_dung = e.read().decode("utf-8")
        try:
            return e.code, json.loads(noi_dung)
        except ValueError:
            return e.code, noi_dung


def loi(than):
    """Câu lỗi dễ đọc từ thân ProblemDetails của ASP.NET Core."""
    if isinstance(than, dict):
        return than.get("detail") or than.get("title") or str(than)
    return str(than)


def cho_api(so_giay=90):
    """Chờ backend lên. Thoát hẳn nếu quá hạn — chạy tiếp cũng chỉ ra một rừng lỗi kết nối."""
    for _ in range(so_giay):
        try:
            urllib.request.urlopen(GOC + "/api/he-thong/kiem-tra-ket-noi", timeout=5)
            return
        except Exception:
            time.sleep(1)
    sys.exit(f"Backend không lên ở {GOC} sau {so_giay} giây. Chạy backend rồi thử lại.")


def dang_nhap(ten):
    st, b = goi("POST", "/api/auth/login", than={"username": ten, "password": MAT_KHAU})
    if st != 200:
        sys.exit(f"Đăng nhập {ten} thất bại: HTTP {st} — {loi(b)}")
    return b["accessToken"]


def dang_nhap_nhieu(ds_ten):
    """Đăng nhập một loạt tài khoản, trả về dict {tên: token}."""
    return {ten: dang_nhap(ten) for ten in ds_ten}
