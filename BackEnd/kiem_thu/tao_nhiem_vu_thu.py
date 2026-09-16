"""Dựng bộ nhiệm vụ để thử tay trên giao diện.

Đi qua đúng API thật, không chèn thẳng vào database — nhờ vậy phần AI đoán phòng và trích
kỹ năng lúc tạo cũng chạy, giống hệt khi người dùng bấm nút.

    python tao_nhiem_vu_thu.py          dựng bộ thử (xoá bộ cũ trước)
    python tao_nhiem_vu_thu.py --don    chỉ dọn, không dựng

KHÔNG đánh dấu nhiệm vụ thử bằng tiền tố trong tiêu đề. Đã thử "[THỬ] " và phát hiện chính
mấy ký tự đó làm lệch kết quả AI: cùng một nội dung, có tiền tố thì "Rà soát và cải thiện quy
trình nội bộ" nhảy từ LƯỠNG LỰ sang CHẮC CHẮN, vì tiêu đề là đầu vào của mô hình nhúng. Công
cụ đo mà làm nhiễu phép đo thì vô dụng. Thay vào đó, nhận diện bằng đúng danh sách tiêu đề
trong BO_THU dưới đây — cũng là danh sách mà 26_don_du_lieu_thu.sql dùng, nên hai bên luôn khớp.
"""
import sys

from chung import cho_api, dang_nhap, goi, loi

NGUOI_GIAO_VIEC = ["giamdoc", "tp.phattrien", "tp.nhansu", "tp.hanhchinh", "leader.be", "leader.fe"]


# ---------------------------------------------------------------------------
# Bộ kịch bản. Mỗi phần tử: (nhóm, tiêu đề, mô tả, ưu tiên, hạn, kịch bản, ghi chú kỳ vọng)
#
# "kịch bản" nói nhiệm vụ được đẩy tới đâu trong vòng đời:
#   None                       để nguyên Mới tạo, chưa giao
#   ("giao", id)               giao rồi dừng ở Đã giao
#   ("lam", id, user, %)       giao, người nhận tiếp nhận và cập nhật tiến độ
#   ("bao_cao", id, user)      ... rồi gửi báo cáo, dừng ở Chờ xác nhận
#   ("tu_choi", id, user)      ... rồi bị trả lại, dừng ở Yêu cầu bổ sung
#   ("xong", id, user)         ... rồi được duyệt 5/5, dừng ở Hoàn thành
# ---------------------------------------------------------------------------
BO_THU = [
    ("A. Sáu trạng thái của vòng đời nhiệm vụ",
     "leader.be", "Viết tài liệu API cho cổng thanh toán",
     "Mô tả các điểm cuối, tham số và mã lỗi của cổng thanh toán. Xuất bản dạng OpenAPI.",
     "MEDIUM", "2026-10-15", None,
     "Mới tạo, chưa giao — bấm Gợi ý để xem AI chấm, rồi tự giao"),

    (None, "leader.be", "Thêm bộ nhớ đệm Redis cho truy vấn danh mục",
     "Danh mục kỹ năng bị truy vấn lại mỗi lần tải trang. Thêm lớp đệm và cơ chế làm mới.",
     "HIGH", "2026-10-10", ("giao", 9),
     "Đã giao — đăng nhập nv.giang để bấm Tiếp nhận"),

    (None, "leader.be", "Chuyển tầng truy cập dữ liệu sang truy vấn bất đồng bộ",
     "Còn một số chỗ gọi đồng bộ gây chặn luồng. Rà soát và chuyển hết sang async/await.",
     "MEDIUM", "2026-10-20", ("lam", 7, "nv.cuong", 40),
     "Đang thực hiện 40% — nv.cuong cập nhật tiếp hoặc gửi báo cáo"),

    (None, "leader.be", "Bổ sung kiểm thử tự động cho luồng đăng nhập",
     "Viết kiểm thử cho đăng nhập, làm mới token và đăng xuất. Bao gồm cả ca sai mật khẩu.",
     "MEDIUM", "2026-10-05", ("bao_cao", 8, "nv.dung"),
     "Chờ xác nhận — leader.be vào duyệt, có chấm điểm 1..5"),

    (None, "leader.be", "Tối ưu kích thước gói tải về của trang quản trị",
     "Gói JavaScript đang hơn 2 MB. Tách gói theo tuyến và bỏ thư viện không dùng.",
     "LOW", "2026-10-25", ("tu_choi", 9, "nv.giang"),
     "Yêu cầu bổ sung — nv.giang sửa rồi gửi lại báo cáo"),

    (None, "leader.be", "Sửa lỗi sai múi giờ khi hiển thị hạn hoàn thành",
     "Hạn hiển thị lệch một ngày với người dùng ngoài múi giờ Việt Nam. Chuẩn hoá về UTC.",
     "HIGH", "2026-09-30", ("xong", 7, "nv.cuong"),
     "Hoàn thành, chấm 5/5 — xem màn chi tiết của một việc đã đóng"),

    ("B. AI đoán phòng (tạo chưa giao — xem trường Phòng ban được gắn tự động)",
     "giamdoc", "Xây dựng dịch vụ gửi thông báo đẩy",
     "Viết dịch vụ nhận sự kiện từ hàng đợi rồi đẩy thông báo tới ứng dụng di động. "
     "Dùng C# với ASP.NET Core, lưu trạng thái gửi vào Oracle.",
     "MEDIUM", "2026-11-01", None,
     "CHẮC CHẮN → Phòng Phát triển phần mềm"),

    (None, "giamdoc", "Tổ chức chương trình đào tạo hội nhập cho nhân viên mới",
     "Soạn nội dung đào tạo hội nhập, sắp lịch cho người mới vào quý 4 và thu thập phản hồi "
     "sau khoá học.",
     "MEDIUM", "2026-11-05", None,
     "CHẮC CHẮN → Phòng Nhân sự"),

    (None, "giamdoc", "Tổ chức buổi tổng kết và khen thưởng cuối năm",
     "Lên chương trình, dự trù kinh phí, chuẩn bị danh sách khen thưởng và mời các bộ phận "
     "tham gia.",
     "MEDIUM", "2026-10-31", None,
     "CHẮC CHẮN → Phòng Hành chính - Kế toán"),

    (None, "giamdoc", "Rà soát và cải thiện quy trình nội bộ",
     "Xem lại các quy trình đang dùng, tìm chỗ chồng chéo và đề xuất điều chỉnh.",
     "LOW", "2026-11-20", None,
     "LƯỠNG LỰ — phòng đứng đầu 0,84 nhưng phòng thứ hai 0,75, không đủ cách biệt"),

    (None, "giamdoc", "Chuẩn bị hồ sơ đánh giá năng lực cuối năm",
     "Thu thập số liệu kết quả công việc, tổng hợp theo từng bộ phận và chuẩn bị tài liệu "
     "cho buổi đánh giá.",
     "MEDIUM", "2026-11-10", None,
     "LƯỠNG LỰ — Nhân sự 0,94 so với Hành chính 0,87, hai phòng đều hợp lý"),

    ("C. Thâm niên (mở Tạo nhiệm vụ, bấm Gợi ý, xem cột Thâm niên trong thanh phân rã điểm)",
     "leader.be", "Viết điểm cuối tra cứu lịch sử giao dịch",
     "Thêm API tra cứu lịch sử giao dịch theo khoảng ngày, có phân trang. "
     "Dùng C# và Entity Framework Core trên Oracle.",
     "MEDIUM", "2026-10-18", None,
     "Tuấn (vào 17/08/2026) phải xếp dưới Giang và Cường"),

    (None, "tp.phattrien", "Thiết kế lại kiến trúc phân tầng cho hệ thống lõi",
     "Đánh giá kiến trúc hiện tại, đề xuất phân tầng mới và lộ trình chuyển đổi không gián "
     "đoạn dịch vụ. Cần kinh nghiệm dẫn dắt kỹ thuật.",
     "HIGH", "2026-12-01", None,
     "Tuấn không được lọt nhóm đầu. Lưu ý: điểm thâm niên bão hoà ở 2 năm nên "
     "KHÔNG phân biệt giữa những người đã 2+ năm — thứ tự giữa họ do các thành phần khác quyết"),

    ("D. Phân quyền",
     "leader.be", "Kiểm tra phạm vi giao việc của trưởng nhóm",
     "Đăng nhập leader.be rồi mở ô Người thực hiện — chỉ được thấy người trong nhóm Backend.",
     "LOW", "2026-10-28", None,
     "ô Người thực hiện chỉ có Cường, Dung, Giang, Tuấn"),

    (None, "tp.nhansu", "Khảo sát nhu cầu đào tạo năm 2027",
     "Gửi phiếu khảo sát tới toàn bộ nhân viên, tổng hợp nhu cầu đào tạo và lập kế hoạch "
     "ngân sách.",
     "MEDIUM", "2026-11-15", None,
     "chỉ thấy Lan và Minh"),
]

TIEU_DE_THU = {x[2] for x in BO_THU}

# ---------------------------------------------------------------------------
cho_api()
tk = {u: dang_nhap(u) for u in
      ["giamdoc", "tp.phattrien", "tp.nhansu", "tp.hanhchinh", "leader.be", "leader.fe",
       "nv.cuong", "nv.dung", "nv.giang", "nv.hoa", "nv.lan", "nv.nga", "nv.tuan"]}


def don_bo_cu():
    """Xoá nhiệm vụ thử của lần chạy trước.

    API chỉ cho xoá khi nhiệm vụ còn "Mới tạo" và chưa có tiến độ hay báo cáo. Phần đã đẩy
    sâu hơn trong vòng đời phải dọn bằng 26_don_du_lieu_thu.sql. In rõ phần còn lại để không
    ai quên — việc còn mở sẽ cộng vào khối lượng của người nhận, mà khối lượng là một thành
    phần chấm điểm của AI.
    """
    st, b = goi("GET", "/api/nhiem-vu?kichThuocTrang=500", tk["giamdoc"])
    cu = [t for t in (b["danhSach"] if st == 200 else []) if t["title"] in TIEU_DE_THU]
    if not cu:
        return
    xoa, con = 0, []
    for t in cu:
        for u in NGUOI_GIAO_VIEC:
            if goi("DELETE", f"/api/nhiem-vu/{t['id']}", tk[u])[0] == 204:
                xoa += 1
                break
        else:
            con.append(t["id"])
    print(f"Dọn bộ cũ: xoá {xoa}/{len(cu)} nhiệm vụ thử.")
    if con:
        print(f"   Còn {len(con)} việc đã đi sâu vào vòng đời, API không xoá được: {con}")
        print("   Chạy BackEnd/db/26_don_du_lieu_thu.sql để dọn nốt, rồi chạy lại script này.")


def tao(nguoi_tao, tieu_de, mo_ta, uu_tien, han):
    than = {"title": tieu_de, "description": mo_ta, "priority": uu_tien, "dueDate": han}
    st, b = goi("POST", "/api/nhiem-vu", tk[nguoi_tao], than)
    if st != 201:
        print(f"   !! không tạo được {tieu_de!r}: HTTP {st} {loi(b)}")
        return None
    return b


def dien_kich_ban(nguoi_tao, task_id, kb):
    """Đẩy nhiệm vụ tới đúng trạng thái mà kịch bản yêu cầu."""
    if kb is None:
        return True
    loai = kb[0]
    nguoi_nhan = kb[1]
    if goi("POST", f"/api/nhiem-vu/{task_id}/giao", tk[nguoi_tao], {"assigneeId": nguoi_nhan})[0] != 200:
        print(f"   !! không giao được #{task_id}")
        return False
    if loai == "giao":
        return True

    ai_lam = kb[2]
    if goi("POST", f"/api/nhiem-vu/{task_id}/tiep-nhan", tk[ai_lam])[0] != 200:
        print(f"   !! {ai_lam} không tiếp nhận được #{task_id}")
        return False

    phan_tram = kb[3] if loai == "lam" else (90 if loai == "tu_choi" else 100)
    goi("POST", f"/api/nhiem-vu/{task_id}/tien-do", tk[ai_lam],
        {"progressPercent": phan_tram, "content": "Cập nhật tiến độ trong lúc thực hiện."})
    if loai == "lam":
        return True

    st, bc = goi("POST", f"/api/nhiem-vu/{task_id}/bao-cao", tk[ai_lam],
                 {"content": "Đã hoàn thành phần việc được giao, kèm mô tả kết quả và cách kiểm chứng."})
    if st != 200:
        print(f"   !! {ai_lam} không gửi được báo cáo cho #{task_id}")
        return False
    if loai == "bao_cao":
        return True

    if loai == "tu_choi":
        goi("POST", f"/api/bao-cao/{bc['id']}/duyet", tk[nguoi_tao],
            {"xacNhan": False, "reviewNote": "Chưa đạt yêu cầu đề ra, xem lại phần còn thiếu rồi gửi lại."})
    else:
        goi("POST", f"/api/bao-cao/{bc['id']}/duyet", tk[nguoi_tao],
            {"xacNhan": True, "reviewNote": "Làm gọn, có kiểm chứng kèm theo.",
             "qualityScore": 5, "completionScore": 5})
    return True


don_bo_cu()
if "--don" in sys.argv:
    print("Đã dọn xong, không dựng bộ mới.")
    sys.exit(0)

print()
da_tao = []
for nhom, nguoi_tao, tieu_de, mo_ta, uu_tien, han, kb, ky_vong in BO_THU:
    if nhom:
        print(nhom if not da_tao else "\n" + nhom)
    nv = tao(nguoi_tao, tieu_de, mo_ta, uu_tien, han)
    if not nv:
        continue
    dien_kich_ban(nguoi_tao, nv["id"], kb)
    da_tao.append(nv["id"])
    print(f"  #{nv['id']:>3}  {tieu_de[:44]:<44} {ky_vong}")

print()
print("=" * 78)
print(f"Đã dựng {len(da_tao)} nhiệm vụ thử: {da_tao}")
print()
print("Đăng nhập — mật khẩu mọi tài khoản: 123456")
print("  giamdoc       Giám đốc         thấy tất cả, giao được cho mọi người")
print("  tp.phattrien  TP Phát triển    phạm vi phòng Phát triển phần mềm")
print("  leader.be     Trưởng nhóm BE   phạm vi nhóm Backend")
print("  nv.giang      Nhân viên        chỉ thấy việc của mình")
print("  nv.tuan       Nhân viên mới    vào làm 17/08/2026, chưa có lịch sử")
print()
print("Dọn lại khi thử xong:")
print("  python tao_nhiem_vu_thu.py --don")
print("  sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @26_don_du_lieu_thu.sql")
