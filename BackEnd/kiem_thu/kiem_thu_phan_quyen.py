"""Kiểm thử phân quyền theo cơ cấu tổ chức trên API thật.

Số liệu mong đợi lấy từ truy vấn SQL độc lập (kt10.sql), không suy từ code đang kiểm.
Bộ này CÓ ghi dữ liệu (tạo nhiệm vụ, duyệt báo cáo) — chạy xong phải nạp lại 22 rồi 24.
"""
import sys

from chung import cho_api, dang_nhap, goi, loi

KET_QUA: list[tuple[bool, str, str]] = []

# Mọi nhiệm vụ bộ này tạo ra, để cuối buổi dọn đi. Không dọn thì chúng nằm lại ở trạng thái MỞ
# và cộng vào khối lượng việc đang gánh của người nhận — mà khối lượng là một thành phần chấm
# điểm, nên rác tích lại sẽ bóp méo cả thứ hạng gợi ý lẫn số liệu đánh giá mô hình.
DA_TAO: list[tuple[int, str]] = []


def kiem(ten, dat, chi_tiet=""):
    KET_QUA.append((bool(dat), ten, str(chi_tiet)))


def tao_thang(u, than):
    """Tạo nhiệm vụ và ghi vào sổ dọn dẹp. Dùng cho những chỗ gọi thẳng POST /api/nhiem-vu."""
    st, b = goi("POST", "/api/nhiem-vu", tk[u], than)
    if st == 201 and isinstance(b, dict) and b.get("id"):
        DA_TAO.append((b["id"], u))
    return st, b


def don_dep():
    """Xoá những nhiệm vụ bộ này vừa tạo.

    API chỉ cho xoá khi nhiệm vụ còn "Mới tạo" và chưa có dữ liệu con, nên những việc đã giao
    thành công thì phải dọn bằng BackEnd/db/25_don_rac_kiem_thu.sql.
    """
    xoa, con = 0, []
    for tid, u in DA_TAO:
        st, _ = goi("DELETE", f"/api/nhiem-vu/{tid}", tk[u])
        if st == 204:
            xoa += 1
        else:
            con.append(tid)
    print()
    print(f"Dọn dẹp: đã xoá {xoa}/{len(DA_TAO)} nhiệm vụ do bộ kiểm thử tạo.")
    if con:
        print(f"   Còn {len(con)} việc đã giao nên API không xoá được: {con}")
        print("   Chạy BackEnd/db/25_don_rac_kiem_thu.sql để dọn nốt.")


def tong_so(trang):
    for k in ("tongSoDong", "tongSo", "totalCount"):
        if k in trang:
            return trang[k]
    raise KeyError(f"không thấy trường tổng số trong {list(trang)}")



cho_api()
tk = {u: dang_nhap(u) for u in
      ["giamdoc", "tp.phattrien", "tp.nhansu", "leader.be", "leader.fe", "nv.cuong", "nv.tuan"]}

# ---------------------------------------------------------------------------
# Kiểm dịch vụ nhúng trước khi chạy
# ---------------------------------------------------------------------------
# Thiếu dịch vụ nhúng thì backend vẫn chạy bằng TF-IDF dự phòng — đúng thiết kế, nhưng phần
# đoán phòng kém chính xác hơn hẳn và vài ca mục G sẽ trượt. Không báo thì người chạy mất công
# đi tìm lỗi trong mã, trong khi nguyên nhân chỉ là một dịch vụ chưa bật.
st, _cau_hinh = goi("GET", "/api/goi-y/cau-hinh", tk["giamdoc"])
CO_NHUNG = st == 200 and _cau_hinh.get("dichVuNhung", {}).get("sanSang") is True
if not CO_NHUNG:
    ly_do = _cau_hinh.get("dichVuNhung", {}).get("loi") if st == 200 else f"HTTP {st}"
    print()
    print("!! DỊCH VỤ NHÚNG KHÔNG SẴN SÀNG — đang chạy bằng TF-IDF dự phòng.")
    print(f"   Lý do: {ly_do}")
    print("   Các ca đoán phòng ở mục G nhiều khả năng sẽ trượt. Đây KHÔNG phải lỗi mã nguồn.")
    print("   Bật lại: cd BackEnd/ai && .venv/Scripts/python -m uvicorn app:app --port 8000")
    print()

# ---------------------------------------------------------------------------
# A. Mỗi người thấy bao nhiêu nhiệm vụ — so với SQL độc lập
# ---------------------------------------------------------------------------
# Danh sách chỉ hiện VIỆC CỦA CHÍNH MÌNH: mình tạo, hoặc giao cho mình. Không còn phạm vi theo
# phòng hay theo nhóm — giám đốc cũng chỉ thấy việc của giám đốc.
#
# Số liệu nền đếm độc lập bằng SQL trên 90 nhiệm vụ mẫu:
#   SELECT u.USERNAME, COUNT(*) FROM TASKS t
#     JOIN USERS u ON u.ID = t.CREATOR_ID OR u.ID = t.ASSIGNEE_ID
#    WHERE t.ID <= 90 GROUP BY u.USERNAME;
#
# Việc tạo thêm qua giao diện cũng phải được đếm, nên mỗi kỳ vọng được cộng thêm đúng số việc
# ngoài dữ liệu mẫu (id > 90) mà CHÍNH người đó nhìn thấy. Nhờ vậy bộ kiểm thử vẫn kiểm đúng quy
# tắc phạm vi mà không sai mỗi khi có người dùng thật tạo một nhiệm vụ mới.
MAU_CUOI = 90
mong_doi = {"giamdoc": 7, "tp.phattrien": 16, "tp.nhansu": 16, "leader.be": 33,
            "leader.fe": 19, "nv.cuong": 13, "nv.tuan": 0}

for u, n in mong_doi.items():
    st, b = goi("GET", "/api/nhiem-vu?kichThuocTrang=500", tk[u])
    thuc = tong_so(b) if st == 200 else f"HTTP {st}"
    bu = len([t for t in (b["danhSach"] if st == 200 else []) if t["id"] > MAU_CUOI])
    kiem(f"A  {u:<13} thấy {n} nhiệm vụ mẫu" + (f" + {bu} ngoài mẫu" if bu else ""),
         thuc == n + bu, f"thực tế {thuc}")

# ---------------------------------------------------------------------------
# B. Danh sách người nhận được việc
# ---------------------------------------------------------------------------
nhan = {"giamdoc": set(range(2, 17)), "tp.phattrien": {5, 6, 7, 8, 9, 10, 11, 16},
        "leader.be": {7, 8, 9, 16}}
for u, ds in nhan.items():
    st, b = goi("GET", "/api/nguoi-dung/nhan-vien", tk[u])
    thuc = {x["id"] for x in b} if st == 200 else f"HTTP {st}"
    kiem(f"B  {u:<13} giao được cho {sorted(ds)}", thuc == ds, f"thực tế {sorted(thuc) if isinstance(thuc, set) else thuc}")

st, b = goi("GET", "/api/nguoi-dung/nhan-vien", tk["leader.be"])
co_nhom = all(x.get("tenNhom") == "Nhóm Backend" for x in b) if st == 200 else False
kiem("B  danh sách người nhận có kèm tên nhóm", co_nhom, [x.get("tenNhom") for x in b] if st == 200 else st)

st, _ = goi("GET", "/api/nguoi-dung/nhan-vien", tk["nv.cuong"])
kiem("B  nhân viên không gọi được danh sách người nhận", st == 403, f"HTTP {st}")

# ---------------------------------------------------------------------------
# C. Xem chi tiết
# ---------------------------------------------------------------------------
# #1: leader.be tạo, nv.cuong nhận. #55: leader.be tạo, chưa giao ai.
# Trưởng phòng Phát triển không dính tới cả hai nên không xem được nữa, dù cùng phòng.
for u, id_, ma in [("leader.fe", 1, 403), ("tp.nhansu", 1, 403), ("nv.cuong", 2, 403),
                   ("tp.phattrien", 1, 403), ("tp.phattrien", 55, 403), ("leader.be", 1, 200),
                   ("nv.cuong", 1, 200), ("giamdoc", 1, 403),
                   ("giamdoc", 9999, 404)]:
    st, _ = goi("GET", f"/api/nhiem-vu/{id_}", tk[u])
    kiem(f"C  {u:<13} xem #{id_} -> {ma}", st == ma, f"HTTP {st}")

st, b = goi("GET", "/api/nhiem-vu/1", tk["leader.be"])
kiem("C  chi tiết #1 có phòng và nhóm",
     st == 200 and b.get("tenPhongBan") == "Phòng Phát triển phần mềm" and b.get("tenNhom") == "Nhóm Backend",
     f"{b.get('tenPhongBan')} / {b.get('tenNhom')}" if st == 200 else st)

for u, ma in [("leader.fe", 403), ("leader.be", 200)]:
    st, _ = goi("GET", "/api/nhiem-vu/1/tien-do", tk[u])
    kiem(f"C  {u:<13} xem tiến độ #1 -> {ma}", st == ma, f"HTTP {st}")
    st, _ = goi("GET", "/api/nhiem-vu/1/bao-cao", tk[u])
    kiem(f"C  {u:<13} xem báo cáo #1 -> {ma}", st == ma, f"HTTP {st}")

# ---------------------------------------------------------------------------
# D. Quy tắc giao việc
# ---------------------------------------------------------------------------
def tao(u, assignee=None, tieu_de="Kiểm thử phân quyền"):
    than = {"title": tieu_de, "description": "Dữ liệu kiểm thử, sẽ bị xoá khi nạp lại.", "priority": "LOW"}
    if assignee is not None:
        than["assigneeId"] = assignee
    st, b = goi("POST", "/api/nhiem-vu", tk[u], than)
    if st == 201 and isinstance(b, dict) and b.get("id"):
        DA_TAO.append((b["id"], u))
    return st, b

st, b = tao("leader.be", 10)
kiem("D  trưởng nhóm Backend giao cho người nhóm Frontend -> 400",
     st == 400 and "trong nhóm mình" in loi(b), f"HTTP {st}: {loi(b)}")

st, b = tao("leader.be", 2)
kiem("D  trưởng nhóm giao ngược lên trưởng phòng -> 400",
     st == 400 and "cấp dưới" in loi(b), f"HTTP {st}: {loi(b)}")

st, b = tao("tp.phattrien", 12)
kiem("D  trưởng phòng Phát triển giao cho người phòng Nhân sự -> 400",
     st == 400 and "phòng khác" in loi(b), f"HTTP {st}: {loi(b)}")

st, b = tao("leader.be", 8)
kiem("D  trưởng nhóm Backend giao cho Dung -> 201, tự gắn phòng 2 nhóm 1",
     st == 201 and b["departmentId"] == 2 and b["teamId"] == 1 and b["statusCode"] == "DA_GIAO",
     f"HTTP {st}" + (f", phòng {b.get('departmentId')} nhóm {b.get('teamId')} {b.get('statusCode')}" if st == 201 else f": {loi(b)}"))

st, b = tao("giamdoc", 3)
id_gd = b["id"] if st == 201 else None
kiem("D  Giám đốc giao cho trưởng phòng Nhân sự -> 201, phòng 3, không nhóm",
     st == 201 and b["departmentId"] == 3 and b["teamId"] is None,
     f"HTTP {st}" + (f", phòng {b.get('departmentId')} nhóm {b.get('teamId')}" if st == 201 else f": {loi(b)}"))

if id_gd:
    st, b = goi("POST", f"/api/nhiem-vu/{id_gd}/giao", tk["giamdoc"], {"assigneeId": 14})
    kiem("D  đổi người khi còn DA_GIAO sang phòng khác -> phòng thực thi đổi theo",
         st == 200 and b["departmentId"] == 4 and b["assigneeId"] == 14,
         f"HTTP {st}" + (f", phòng {b.get('departmentId')}" if st == 200 else f": {loi(b)}"))

st, b = tao("leader.be")
id_nhap = b["id"] if st == 201 else None
kiem("D  tạo không giao -> MOI_TAO", st == 201 and b["statusCode"] == "MOI_TAO", f"HTTP {st}")
if id_nhap:
    st, b = goi("POST", f"/api/nhiem-vu/{id_nhap}/giao", tk["leader.be"], {"assigneeId": 10})
    kiem("D  giao nhiệm vụ nháp cho người ngoài nhóm -> 400", st == 400, f"HTTP {st}: {loi(b)}")
    st, b = goi("POST", f"/api/nhiem-vu/{id_nhap}/giao", tk["leader.be"], {"assigneeId": 9})
    kiem("D  giao nhiệm vụ nháp cho Giang -> 200, phòng 2 nhóm 1",
         st == 200 and b["departmentId"] == 2 and b["teamId"] == 1, f"HTTP {st}")

# ---------------------------------------------------------------------------
# E. Tập ứng viên của AI khớp đúng phạm vi giao việc
# ---------------------------------------------------------------------------
for u, n, pham_vi in [("leader.be", 4, {7, 8, 9, 16}), ("tp.nhansu", 2, {12, 13}),
                      ("giamdoc", 15, set(range(2, 17)))]:
    st, b = goi("POST", "/api/goi-y/nguoi-thuc-hien", tk[u],
                {"title": "Xây dựng API quản lý danh mục", "description": "ASP.NET Core và Oracle", "soLuong": 50})
    if st != 200:
        kiem(f"E  {u:<13} AI xét {n} ứng viên", False, f"HTTP {st}: {loi(b)}")
        continue
    ids = {x["userId"] for x in b["ungVien"]}
    kiem(f"E  {u:<13} phạm vi {n} người, ứng viên AI trả về đều trong phạm vi",
         b["soUngVienTrongPhamVi"] == n and ids <= pham_vi,
         f"phạm vi {b['soUngVienTrongPhamVi']}, xét {b['soUngVienDaXet']}, id {sorted(ids)}")
    if u == "giamdoc":
        phong_pt = {2, 5, 6, 7, 8, 9, 10, 11, 16}
        kiem("E  giamdoc: tầng 1 đoán việc ASP.NET Core thuộc phòng Phát triển, chỉ xét người phòng đó",
             b["suyLuanPhongBan"]["ketLuan"] == "CHAC_CHAN" and ids <= phong_pt and b["soUngVienDaXet"] < n,
             f"{b['suyLuanPhongBan']['ketLuan']}, xét {b['soUngVienDaXet']}, id {sorted(ids)}")

# ---------------------------------------------------------------------------
# F. Duyệt báo cáo kèm điểm
# ---------------------------------------------------------------------------
def bao_cao_dang_cho(task_id, u):
    """Báo cáo còn chờ duyệt của một nhiệm vụ, hoặc None nếu lần chạy trước đã duyệt mất.

    Bộ này ghi dữ liệu nên chạy lần hai mà chưa nạp lại 22/24 thì fixture không còn. Trả None
    để phần F báo đúng lý do, thay vì ném StopIteration làm sập cả bộ ở giữa chừng và giấu mất
    kết quả của tất cả những phần sau.
    """
    st, b = goi("GET", f"/api/nhiem-vu/{task_id}", tk[u])
    if st != 200:
        return None
    return next((x["id"] for x in b["danhSachBaoCao"] if x["reportStatus"] == "CHO_XAC_NHAN"), None)

bc33 = bao_cao_dang_cho(33, "leader.be")
bc68 = bao_cao_dang_cho(68, "tp.nhansu")
con_fixture = bc33 is not None and bc68 is not None
if not con_fixture:
    thieu = ", ".join(n for n, v in (("#33", bc33), ("#68", bc68)) if v is None)
    print()
    print(f"!! BỎ QUA phần F: {thieu} không còn báo cáo chờ duyệt — lần chạy trước đã duyệt mất.")
    print("   Nạp lại 22_seed_nhiem_vu.sql rồi 24_seed_v2.sql để chạy được đủ bộ.")
    print()
if con_fixture:
    st, b = goi("POST", f"/api/bao-cao/{bc33}/duyet", tk["leader.be"], {"xacNhan": True, "qualityScore": 7})
    kiem("F  điểm chất lượng 7 -> 400", st == 400, f"HTTP {st}: {loi(b)}")

    st, b = goi("POST", f"/api/bao-cao/{bc68}/duyet", tk["tp.phattrien"], {"xacNhan": True, "qualityScore": 4})
    kiem("F  trưởng phòng khác duyệt báo cáo không phải mình giao -> 403", st == 403, f"HTTP {st}")

    st, b = goi("POST", f"/api/bao-cao/{bc33}/duyet", tk["leader.be"],
                {"xacNhan": True, "reviewNote": "Kịch bản khôi phục rõ ràng.", "qualityScore": 5, "completionScore": 4})
    kiem("F  duyệt báo cáo đang chờ của #33 kèm điểm 5/4 -> 200",
         st == 200 and b.get("qualityScore") == 5 and b.get("completionScore") == 4, f"HTTP {st}: {b if st != 200 else ''}")

    st, b = goi("GET", "/api/nhiem-vu/33", tk["leader.be"])
    bc = next((x for x in b.get("danhSachBaoCao", []) if x["id"] == bc33), {}) if st == 200 else {}
    kiem("F  nhiệm vụ #33 thành HOAN_THANH, điểm lưu xuống DB",
         st == 200 and b["statusCode"] == "HOAN_THANH" and bc.get("qualityScore") == 5,
         f"{b.get('statusCode')} / điểm {bc.get('qualityScore')}" if st == 200 else st)


# ---------------------------------------------------------------------------
# G. AI tự đoán phòng khi tạo nhiệm vụ chưa giao
# ---------------------------------------------------------------------------
bang_luong = {"title": "Lập bảng lương và bảo hiểm tháng 10",
              "description": "Tính lương theo bảng chấm công tháng 10, khấu trừ thuế thu nhập cá nhân và đóng bảo hiểm xã hội.",
              "priority": "HIGH"}
st, b = tao_thang("giamdoc", bang_luong)
kiem("G  Giám đốc tạo việc bảng lương chưa giao -> AI gắn phòng Hành chính (4)",
     st == 201 and b["departmentId"] == 4, f"HTTP {st}, phòng {b.get('departmentId') if st == 201 else loi(b)}")

st, b = tao_thang("leader.be", bang_luong)
kiem("G  Trưởng nhóm Backend tạo việc bảng lương -> phòng đoán ra ngoài phạm vi, KHÔNG gắn",
     st == 201 and b["departmentId"] is None, f"HTTP {st}, phòng {b.get('departmentId') if st == 201 else loi(b)}")

st, b = tao_thang("tp.phattrien",
            {"title": "Tối ưu truy vấn báo cáo doanh thu trên Oracle",
             "description": "Câu truy vấn tổng hợp chạy chậm, cần xem kế hoạch thực thi và bổ sung chỉ mục."})
kiem("G  Trưởng phòng Phát triển tạo việc Oracle -> gắn phòng 2 (nhóm Backend hoặc để trống)",
     st == 201 and b["departmentId"] == 2 and b["teamId"] in (None, 1),
     f"HTTP {st}, phòng {b.get('departmentId')} nhóm {b.get('teamId')}" if st == 201 else loi(b))
if st == 201:
    st2, g = goi("POST", "/api/goi-y/nguoi-thuc-hien", tk["tp.phattrien"], {"taskId": b["id"], "soLuong": 3})
    kiem("G  kỹ năng AI trích lúc tạo được lưu lại và dùng khi gợi ý",
         st2 == 200 and len(g["kyNangYeuCau"]) > 0 and all(k["nguon"] == "AI" for k in g["kyNangYeuCau"]),
         [k["ten"] + "/" + k["nguon"] for k in g["kyNangYeuCau"]] if st2 == 200 else st2)
    kiem("G  gợi ý cho việc Oracle -> Giang đứng đầu",
         st2 == 200 and g["ungVien"][0]["userId"] == 9, g["ungVien"][0]["fullName"] if st2 == 200 else st2)

# ---------------------------------------------------------------------------
# H. Chuỗi giao việc: danh sách hẹp, quyền xem thông cả hai chiều
# ---------------------------------------------------------------------------
# Giám đốc giao xuống trưởng phòng, trưởng phòng giao tiếp xuống nhân viên. Việc con KHÔNG hiện
# trong danh sách của giám đốc, nhưng giám đốc vẫn mở được chi tiết để biết lệnh của mình đang
# nằm ở tay ai. Ngược lại nhân viên bấm lên được nhiệm vụ cha — nếu chặn chiều này thì chính
# khối "Chuỗi giao việc" trên màn hình của họ lại dẫn tới trang báo không có quyền.
st, cha_h = tao_thang("giamdoc", {
    "title": "Kiểm thử chuỗi giao việc", "description": "Việc gốc do giám đốc giao",
    "priority": "MEDIUM", "assigneeId": 2})

if st == 201:
    st, con_h = goi("POST", f"/api/nhiem-vu/{cha_h['id']}/giao-tiep-xuong", tk["tp.phattrien"], {
        "title": "Kiểm thử chuỗi giao việc — phần con", "priority": "MEDIUM", "assigneeId": 7})
    kiem("H  trưởng phòng giao tiếp xuống nhân viên -> 201", st == 201,
         f"HTTP {st}: {loi(con_h)}")

    if st == 201:
        DA_TAO.append((con_h["id"], "tp.phattrien"))

        st, b = goi("GET", "/api/nhiem-vu?kichThuocTrang=500", tk["giamdoc"])
        trong_ds = any(t["id"] == con_h["id"] for t in (b["danhSach"] if st == 200 else []))
        kiem("H  nhiệm vụ con KHÔNG hiện trong danh sách của giám đốc", not trong_ds,
             f"có trong danh sách: {trong_ds}")

        st, _ = goi("GET", f"/api/nhiem-vu/{con_h['id']}", tk["giamdoc"])
        kiem("H  giám đốc mở được chi tiết nhiệm vụ con (xuôi theo chuỗi) -> 200",
             st == 200, f"HTTP {st}")

        st, _ = goi("GET", f"/api/nhiem-vu/{cha_h['id']}", tk["nv.cuong"])
        kiem("H  nhân viên mở được nhiệm vụ cha (ngược theo chuỗi) -> 200",
             st == 200, f"HTTP {st}")

        st, b = goi("GET", "/api/nhiem-vu?kichThuocTrang=500", tk["nv.cuong"])
        trong_ds = any(t["id"] == cha_h["id"] for t in (b["danhSach"] if st == 200 else []))
        kiem("H  nhiệm vụ cha KHÔNG hiện trong danh sách của nhân viên", not trong_ds,
             f"có trong danh sách: {trong_ds}")

        st, _ = goi("GET", f"/api/nhiem-vu/{con_h['id']}", tk["tp.nhansu"])
        kiem("H  người ngoài chuỗi không mở được -> 403", st == 403, f"HTTP {st}")

# ---------------------------------------------------------------------------
# I. Dừng, huỷ và giao tiếp xuống — ranh giới giữa hai thao tác
# ---------------------------------------------------------------------------
# Khoá lại một lỗi thật: trước 06/10/2026, DungDuoc() dùng chung cho cả tạm dừng lẫn huỷ, nên
# tạm dừng HAI LẦN ghi đè PREV_STATUS_CODE bằng chính TAM_DUNG. Mở lại sau đó đặt
# StatusCode = TAM_DUNG với StopReason = NULL, vi phạm CK_TASKS_LY_DO_DUNG, và nhiệm vụ KẸT
# VĨNH VIỄN — lần nào mở cũng HTTP 409.
#
# Bốn ca đầu kiểm cái đã sửa; bốn ca sau kiểm những thứ PHẢI VẪN CHẠY, vì chỗ sửa nằm đúng
# trên đường đi của chúng.


def _tao_da_nhan(hau_to):
    """Tạo nhiệm vụ giao trưởng phòng Phát triển rồi cho tiếp nhận, trả về id."""
    st, b = tao_thang("giamdoc", {
        "title": "Kiểm thử dừng " + hau_to, "description": "ranh giới dừng/huỷ",
        "priority": "MEDIUM", "assigneeId": 2})
    if st != 201:
        return None
    goi("POST", f"/api/nhiem-vu/{b['id']}/tiep-nhan", tk["tp.phattrien"])
    return b["id"]


_a = _tao_da_nhan("chồng dừng")
if _a:
    goi("POST", f"/api/nhiem-vu/{_a}/tam-dung", tk["giamdoc"],
        {"lyDo": "Lần một.", "kemNhiemVuCon": False})
    st, b = goi("POST", f"/api/nhiem-vu/{_a}/tam-dung", tk["giamdoc"],
                {"lyDo": "Lần hai.", "kemNhiemVuCon": False})
    kiem("I  tạm dừng việc ĐANG tạm dừng -> 400", st == 400, f"HTTP {st}: {loi(b)[:60]}")

    st, b = goi("POST", f"/api/nhiem-vu/{_a}/mo-lai", tk["giamdoc"])
    kiem("I  mở lại về đúng trạng thái trước khi dừng",
         st == 200 and b.get("tenTrangThai") == "Đang thực hiện",
         b.get("tenTrangThai") if st == 200 else f"HTTP {st}: {loi(b)[:50]}")

_b = _tao_da_nhan("huỷ khi đang dừng")
if _b:
    goi("POST", f"/api/nhiem-vu/{_b}/tam-dung", tk["giamdoc"],
        {"lyDo": "Dừng để cân nhắc.", "kemNhiemVuCon": False})
    st, b = goi("POST", f"/api/nhiem-vu/{_b}/huy", tk["giamdoc"],
                {"lyDo": "Cân nhắc xong, bỏ hẳn.", "kemNhiemVuCon": False})
    kiem("I  huỷ hẳn việc đang tạm dừng -> VẪN được",
         st == 200 and b.get("tenTrangThai") == "Đã huỷ",
         b.get("tenTrangThai") if st == 200 else f"HTTP {st}: {loi(b)[:50]}")

_c = _tao_da_nhan("giao tiếp khi dừng")
if _c:
    goi("POST", f"/api/nhiem-vu/{_c}/tam-dung", tk["giamdoc"],
        {"lyDo": "Hoãn.", "kemNhiemVuCon": False})
    st, b = goi("POST", f"/api/nhiem-vu/{_c}/giao-tiep-xuong", tk["tp.phattrien"],
                {"title": "Kiểm thử dừng con", "priority": "MEDIUM", "assigneeId": 7})
    kiem("I  giao tiếp xuống khi nhiệm vụ cha đang dừng -> 400", st == 400,
         f"HTTP {st}: {loi(b)[:60]}")

    goi("POST", f"/api/nhiem-vu/{_c}/mo-lai", tk["giamdoc"])
    st, b = goi("POST", f"/api/nhiem-vu/{_c}/giao-tiep-xuong", tk["tp.phattrien"],
                {"title": "Kiểm thử dừng con", "priority": "MEDIUM", "assigneeId": 7})
    kiem("I  mở lại rồi giao tiếp xuống -> 201", st == 201, f"HTTP {st}: {loi(b)[:60]}")
    _con = b.get("id") if st == 201 else None

    if _con:
        DA_TAO.append((_con, "tp.phattrien"))
        goi("POST", f"/api/nhiem-vu/{_con}/tiep-nhan", tk["nv.cuong"])
        goi("POST", f"/api/nhiem-vu/{_con}/tam-dung", tk["tp.phattrien"],
            {"lyDo": "Trưởng phòng tự dừng vì lý do riêng.", "kemNhiemVuCon": False})

        goi("POST", f"/api/nhiem-vu/{_c}/tam-dung", tk["giamdoc"],
            {"lyDo": "Giám đốc dừng.", "kemNhiemVuCon": True})
        st, b = goi("GET", f"/api/nhiem-vu/{_con}", tk["tp.phattrien"])
        kiem("I  tạm dừng cha KHÔNG ghi đè lý do riêng của con",
             st == 200 and "lý do riêng" in str(b.get("lyDoDung", "")),
             str(b.get("lyDoDung"))[:60] if st == 200 else f"HTTP {st}")

        goi("POST", f"/api/nhiem-vu/{_c}/mo-lai", tk["giamdoc"])
        st, b = goi("GET", f"/api/nhiem-vu/{_con}", tk["tp.phattrien"])
        kiem("I  mở lại cha KHÔNG mở nhầm con tự dừng",
             st == 200 and b.get("tenTrangThai") == "Tạm dừng",
             b.get("tenTrangThai") if st == 200 else f"HTTP {st}")

        goi("POST", f"/api/nhiem-vu/{_c}/huy", tk["giamdoc"],
            {"lyDo": "Bỏ hẳn nhánh này.", "kemNhiemVuCon": True})
        st, b = goi("GET", f"/api/nhiem-vu/{_con}", tk["tp.phattrien"])
        kiem("I  huỷ cha -> con đang tạm dừng cũng bị huỷ theo",
             st == 200 and b.get("tenTrangThai") == "Đã huỷ",
             b.get("tenTrangThai") if st == 200 else f"HTTP {st}")

# ---------------------------------------------------------------------------
dat = sum(1 for d, _, _ in KET_QUA if d)
for d, ten, ct in KET_QUA:
    print(f"{'ĐẠT ' if d else 'LỖI'}  {ten}" + ("" if d else f"   <-- {ct}"))
don_dep()
print(f"\n{dat}/{len(KET_QUA)} đạt")
if not CO_NHUNG:
    print("Chạy không có dịch vụ nhúng — các ca đoán phòng mục G trượt vì lý do này,")
    print("không phải vì mã nguồn hỏng. Bật dịch vụ rồi chạy lại để có con số thật.")
sys.exit(0 if dat == len(KET_QUA) else 1)
