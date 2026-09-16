"""Chạy bộ ca vàng: đối chiếu gợi ý của AI với đáp án do người đặt.

Khác gì so với /api/danh-gia/chay:
  - Bộ đánh giá đo trên LỊCH SỬ đã có, nên chỉ phủ những việc từng xảy ra.
  - Bộ này đo trên tình huống NGƯỜI NGHĨ RA, nên phủ được cả việc đời thường chưa từng có
    trong dữ liệu mẫu — đúng chỗ mô hình hay hỏng mà phép đo tự động không thấy.

Hai thứ phải giữ bằng mọi giá:

  1. KHÔNG GHI GÌ XUỐNG DATABASE. Gọi gợi ý bằng title/description chứ không bằng taskId, nên
     không tạo nhiệm vụ nào. Từng dính đúng bẫy này: bộ kiểm thử phân quyền để lại 21 nhiệm vụ
     rác, cộng khống 5 việc vào tải của ba người, suýt làm chọn sai bộ tham số thâm niên.

  2. KHÔNG CHÈN GÌ VÀO VĂN BẢN. Không gắn tiền tố đánh dấu vào tiêu đề. Tiêu đề là đầu vào của
     mô hình nhúng, nên "[THỬ] " đủ làm một ca nhảy từ LƯỠNG LỰ sang CHẮC CHẮN.

Dùng:
    python chay_ca_vang.py              chạy và so với lần trước
    python chay_ca_vang.py --khong-luu  chạy, không ghi đè kết quả lần trước
"""
from __future__ import annotations

import json
import sys
from datetime import datetime
from pathlib import Path

from chung import cho_api, dang_nhap, goi, loi

THU_MUC = Path(__file__).resolve().parent
TEP_CA = THU_MUC / "bo_ca_vang.json"
TEP_KQ = THU_MUC / "ket_qua_ca_vang.json"

SO_UNG_VIEN = 20   # lấy rộng để kiểm được cả kỳ vọng "không được xuất hiện ở hạng nào"


# --------------------------------------------------------------------------- kiểm từng kỳ vọng

def _chuan(s):
    return (s or "").strip().lower()


def kiem_phong(kq, mong):
    cac = kq["suyLuanPhongBan"]["cacPhong"]
    if not cac:
        return False, "AI không chấm được phòng nào"
    dau = cac[0]
    dat = _chuan(mong) in _chuan(dau["ten"])
    return dat, f"phòng đứng đầu: {dau['ten']} ({dau['diem']:.2f})"


def kiem_khong_phong(kq, mong):
    cac = kq["suyLuanPhongBan"]["cacPhong"]
    if not cac:
        return True, "không có phòng nào được chấm"
    dau = cac[0]
    dat = _chuan(mong) not in _chuan(dau["ten"])
    return dat, f"phòng đứng đầu: {dau['ten']} ({dau['diem']:.2f})"


def kiem_ket_luan(kq, mong):
    that = kq["suyLuanPhongBan"]["ketLuan"]
    chap_nhan = [mong] if isinstance(mong, str) else list(mong)
    cac = kq["suyLuanPhongBan"]["cacPhong"]
    # Kèm cách biệt hai phòng đầu: ca sát ngưỡng thì con số này cho biết ngay là nó mong manh.
    cach = f", cách biệt {cac[0]['diem'] - cac[1]['diem']:.3f}" if len(cac) >= 2 else ""
    return that in chap_nhan, f"ra {that}, chấp nhận {'/'.join(chap_nhan)}{cach}"


def kiem_top_n(kq, mong, ten_theo_id):
    n, ai = mong["n"], mong["ai"]
    top = kq["ungVien"][:n]
    id_top = {u["userId"] for u in top}
    can = {uid for uid, ten in ten_theo_id.items() if ten in ai}
    dat = bool(id_top & can)
    return dat, "top %d: %s" % (n, ", ".join(f"{u['thuHang']}.{u['fullName']}" for u in top) or "rỗng")


def kiem_khong_top(kq, mong, ten_theo_id):
    n, ai = mong["n"], mong["ai"]
    top = kq["ungVien"][:n]
    can = {uid for uid, ten in ten_theo_id.items() if ten in ai}
    pham = [u for u in top if u["userId"] in can]
    dat = not pham
    if dat:
        return True, f"không ai trong danh sách lọt top {n}"
    return False, "lọt top %d: %s" % (n, ", ".join(f"{u['thuHang']}.{u['fullName']}" for u in pham))


def kiem_tren(kq, cac_cap, ten_theo_id):
    hang = {u["userId"]: u["thuHang"] for u in kq["ungVien"]}
    id_theo_ten = {ten: uid for uid, ten in ten_theo_id.items()}
    loi_dong = []
    for a, b in cac_cap:
        ha = hang.get(id_theo_ten.get(a))
        hb = hang.get(id_theo_ten.get(b))
        if ha is None or hb is None:
            loi_dong.append(f"{a} hoặc {b} không có trong danh sách")
        elif ha >= hb:
            loi_dong.append(f"{a} hạng {ha} KHÔNG trên {b} hạng {hb}")
    return not loi_dong, "; ".join(loi_dong) or "mọi cặp đều đúng thứ tự"


def kiem_mot_ca(kq, ky_vong, ten_theo_id):
    """Trả về danh sách (tên kỳ vọng, đạt, chi tiết)."""
    ra = []
    if "phong" in ky_vong:
        ra.append(("phòng", *kiem_phong(kq, ky_vong["phong"])))
    if "khongPhong" in ky_vong:
        ra.append(("không phải phòng", *kiem_khong_phong(kq, ky_vong["khongPhong"])))
    if "ketLuan" in ky_vong:
        ra.append(("kết luận", *kiem_ket_luan(kq, ky_vong["ketLuan"])))
    if "topN" in ky_vong:
        ra.append(("top N", *kiem_top_n(kq, ky_vong["topN"], ten_theo_id)))
    if "khongTop" in ky_vong:
        ra.append(("không top", *kiem_khong_top(kq, ky_vong["khongTop"], ten_theo_id)))
    if "tren" in ky_vong:
        ra.append(("thứ tự", *kiem_tren(kq, ky_vong["tren"], ten_theo_id)))
    return ra


# --------------------------------------------------------------------------- chạy

def main():
    cho_api()
    goc = json.loads(TEP_CA.read_text(encoding="utf-8"))
    cac_ca = goc["cac_ca"]

    # Giám đốc thấy toàn bộ nhân sự, dùng để dựng bảng tra tên đăng nhập -> mã người.
    tk_gd = dang_nhap("giamdoc")
    st, ds = goi("GET", "/api/nguoi-dung/nhan-vien", tk_gd)
    if st != 200:
        sys.exit(f"Không lấy được danh sách nhân viên: HTTP {st} — {loi(ds)}")
    ten_theo_id = {u["id"]: u["username"] for u in ds}

    # Vân tay dữ liệu. Tầng đoán phòng đọc lịch sử nhiệm vụ, nên kết quả bộ này PHỤ THUỘC vào
    # trạng thái database. Ghi lại số nhiệm vụ để khi một ca đổi trạng thái còn biết là do sửa
    # mã hay do dữ liệu đã khác.
    st, sk = goi("GET", "/api/he-thong/kiem-tra-ket-noi", tk_gd)
    so_nhiem_vu = next((b["soDong"] for b in (sk or {}).get("cacBang", [])
                        if b["bang"] == "TASKS"), None) if st == 200 else None

    tk = {"giamdoc": tk_gd}
    ket_qua, phien_ban = [], None

    for ca in cac_ca:
        nguoi = ca["nguoiTao"]
        if nguoi not in tk:
            tk[nguoi] = dang_nhap(nguoi)

        st, kq = goi("POST", "/api/goi-y/nguoi-thuc-hien", tk[nguoi],
                     {"title": ca["tieuDe"], "description": ca.get("moTa", ""),
                      "soLuong": SO_UNG_VIEN})
        if st != 200:
            ket_qua.append({"ma": ca["ma"], "trangThai": "LOI", "chiTiet": f"HTTP {st} — {loi(kq)}",
                            "bietTruocLech": ca.get("bietTruocLech", False)})
            continue

        phien_ban = phien_ban or kq.get("phienBanTrongSo")
        muc = kiem_mot_ca(kq, ca.get("kyVong", {}), ten_theo_id)
        dat = all(d for _, d, _ in muc)
        biet_truoc = ca.get("bietTruocLech", False)

        if dat:
            trang_thai = "DAT_NGOAI_DU_KIEN" if biet_truoc else "DAT"
        else:
            trang_thai = "LECH_DA_BIET" if biet_truoc else "LECH"

        ket_qua.append({
            "ma": ca["ma"], "trangThai": trang_thai, "bietTruocLech": biet_truoc,
            "muc": [{"ten": t, "dat": d, "chiTiet": c} for t, d, c in muc],
            "phuongPhap": kq.get("phuongPhap"),
        })

    in_bang(ket_qua, cac_ca)
    so_sanh_lan_truoc(ket_qua, phien_ban, so_nhiem_vu)

    if "--khong-luu" not in sys.argv:
        TEP_KQ.write_text(json.dumps({
            "chayLuc": datetime.now().isoformat(timespec="seconds"),
            "phienBanTrongSo": phien_ban,
            "soNhiemVuTrongDb": so_nhiem_vu,
            "ketQua": ket_qua,
        }, ensure_ascii=False, indent=1), encoding="utf-8")
        print(f"\nĐã lưu {TEP_KQ.name} để lần sau đối chiếu.")

    # Chỉ ca LỆCH ngoài dự kiến mới làm hỏng build; ca đã biết lệch thì không.
    return 1 if any(k["trangThai"] in ("LECH", "LOI") for k in ket_qua) else 0


def in_bang(ket_qua, cac_ca):
    ghi_chu = {c["ma"]: c.get("ghiChu", "") for c in cac_ca}
    nhan = {"DAT": "ĐẠT", "LECH": "LỆCH", "LECH_DA_BIET": "lệch (đã biết)",
            "DAT_NGOAI_DU_KIEN": "ĐẠT (ca đã biết lệch — sửa được rồi!)", "LOI": "LỖI"}
    print(f"\n{'ca':<24}{'kết quả':<34}chi tiết")
    print("-" * 110)
    for k in ket_qua:
        print(f"{k['ma']:<24}{nhan[k['trangThai']]:<34}", end="")
        if k["trangThai"] == "LOI":
            print(k["chiTiet"]); continue
        hong = [m for m in k["muc"] if not m["dat"]]
        print("; ".join(f"{m['ten']}: {m['chiTiet']}" for m in hong) if hong
              else "; ".join(f"{m['ten']} ✔" for m in k["muc"]))

    dat = sum(1 for k in ket_qua if k["trangThai"].startswith("DAT"))
    lech = sum(1 for k in ket_qua if k["trangThai"] == "LECH")
    da_biet = sum(1 for k in ket_qua if k["trangThai"] == "LECH_DA_BIET")
    sua_duoc = sum(1 for k in ket_qua if k["trangThai"] == "DAT_NGOAI_DU_KIEN")
    print("-" * 110)
    print(f"{dat}/{len(ket_qua)} đạt · {lech} lệch ngoài dự kiến · {da_biet} lệch đã biết", end="")
    print(f" · {sua_duoc} ca vừa được sửa" if sua_duoc else "")
    for k in ket_qua:
        if k["trangThai"] == "LECH_DA_BIET" and ghi_chu.get(k["ma"]):
            print(f"  ({k['ma']}) {ghi_chu[k['ma']]}")


def so_sanh_lan_truoc(ket_qua, phien_ban, so_nhiem_vu):
    """Phần quan trọng nhất: cái gì vừa đổi so với lần chạy trước, và có thể do đâu."""
    if not TEP_KQ.exists():
        print("\n(Chưa có kết quả lần trước để đối chiếu — lần sau sẽ có.)")
        return
    cu = json.loads(TEP_KQ.read_text(encoding="utf-8"))
    truoc = {k["ma"]: k["trangThai"] for k in cu["ketQua"]}
    doi = [(k["ma"], truoc.get(k["ma"], "(ca mới)"), k["trangThai"])
           for k in ket_qua if truoc.get(k["ma"], "(ca mới)") != k["trangThai"]]

    print(f"\nSo với lần chạy {cu.get('chayLuc', '?')}:")
    pb_cu, nv_cu = cu.get("phienBanTrongSo"), cu.get("soNhiemVuTrongDb")
    if pb_cu != phien_ban:
        print(f"  tham số:   {pb_cu} -> {phien_ban}")
    if nv_cu != so_nhiem_vu:
        print(f"  dữ liệu:   {nv_cu} -> {so_nhiem_vu} nhiệm vụ trong database")
    if pb_cu == phien_ban and nv_cu == so_nhiem_vu:
        print(f"  tham số và dữ liệu đều không đổi ({phien_ban}, {so_nhiem_vu} nhiệm vụ)")

    if not doi:
        print("  không ca nào đổi trạng thái.")
        return
    for ma, a, b in doi:
        dau = "!!" if b in ("LECH", "LOI") else "->"
        print(f"  {dau} {ma}: {a} -> {b}")
    if nv_cu != so_nhiem_vu:
        print("  Lưu ý: dữ liệu đã đổi, nên thay đổi trên có thể do lịch sử chứ không do sửa mã.")


if __name__ == "__main__":
    sys.exit(main())
