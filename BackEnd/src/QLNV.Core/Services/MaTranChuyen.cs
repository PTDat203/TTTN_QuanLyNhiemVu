using System.Linq;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>
/// §2.4 — MOT DONG cua ma tran chuyen trang thai, dang DU LIEU (khong phai ma thi hanh).
/// Dung cho: man "So do quy trinh" cua FE, tai lieu ban giao va kiem thu doi chieu
/// (test co the duyet <see cref="MaTranChuyen.ToanBo"/> de bao dam khong sot chuyen nao).
/// </summary>
/// <param name="Ma">Ma dong trong §2.4: T1..T14.</param>
/// <param name="Tu">Cap <c>(trangthai, trangthaiDvXuly)</c> nguon, dang van ban dung nhu dac ta.</param>
/// <param name="HanhDong">Ten hanh dong hien thi cho nguoi dung.</param>
/// <param name="Vai">Vai thuc hien: "Nguoi giao" hoac "Nguoi thuc hien".</param>
/// <param name="Den">Cap dich hoac mo ta ket qua.</param>
/// <param name="ThietKeMoi">
/// True neu chuyen nay KHONG co trong he goc (§10.3) ma la thiet ke moi cua app nho.
/// Hien chi co T2 (Tiep nhan).
/// </param>
public sealed record ChuyenTrangThai(
    string Ma,
    string Tu,
    string HanhDong,
    string Vai,
    string Den,
    bool ThietKeMoi)
{
    /// <summary>Ghi chu doi chieu dac ta (so hieu muc + canh bao ban giao). Khong bat buoc.</summary>
    public string GhiChu { get; init; } = string.Empty;
}

/// <summary>§1 — mot buoc trong so do quy trinh 7 buoc.</summary>
/// <param name="So">Chi so buoc, 1..7.</param>
/// <param name="Ten">Ten buoc.</param>
public sealed record BuocQuyTrinh(int So, string Ten);

/// <summary>Mo ta mot hanh dong cua §6.2 — de FE sinh menu ngu canh dung ten co quyen.</summary>
/// <param name="Ma">Ten co quyen (camelCase, trung khoa JSON cua <c>QuyenNhiemVuDto</c>).</param>
/// <param name="Nhan">Nhan hien thi tieng Viet.</param>
/// <param name="Dong">So hieu dong trong bang §6.2 (0 = mo rong, khong co dong nao).</param>
/// <param name="Vai">Vai duoc phep.</param>
public sealed record MoTaHanhDong(string Ma, string Nhan, int Dong, string Vai);

/// <summary>
/// §2.4 — bang 14 dong T1..T14 va §1 so do 7 buoc, duoi dang DU LIEU THUAN.
/// Lop nay KHONG chua logic chuyen trang thai (logic nam o <see cref="NhiemVuStateMachine"/>);
/// no chi de tra cuu, hien thi va doi chieu khi kiem thu.
/// </summary>
public static class MaTranChuyen
{
    private const string VaiNguoiGiao = "Người giao";
    private const string VaiNguoiThucHien = "Người thực hiện";
    private const string VaiTatCaLienQuan = "Tất cả liên quan";

    private static readonly IReadOnlyList<ChuyenTrangThai> _toanBo = new[]
    {
        // §1.2 buoc 2 — trang thai khoi tao do server dat = 3 (§10.4: he goc khong gan o FE).
        new ChuyenTrangThai("T1", "—", "Tạo & giao nhiệm vụ", VaiNguoiGiao, "(3, null)", false)
        {
            GhiChu = "§1.2 bước 2 — trạng thái khởi tạo do server đặt = 3; hệ gốc không gán ở FE (§10.4)."
        },
        // §2.4 T2 — THIET KE MOI hoan toan (§10.3).
        new ChuyenTrangThai("T2", "(3, null)", "Tiếp nhận", VaiNguoiThucHien, "(2, null) + ngaytiepnhan", true)
        {
            GhiChu = "§10.3 — hệ gốc KHÔNG có nút, API hay trạng thái tiếp nhận. Thiết kế hoàn toàn mới."
        },
        new ChuyenTrangThai("T3", "(3, null) hoặc (2, null)", "Từ chối nhiệm vụ", VaiNguoiThucHien, "(6, 10)", false)
        {
            GhiChu = "§10.7 — bắt buộc nhập lý do là yêu cầu MỚI, hệ gốc không bắt buộc."
        },
        new ChuyenTrangThai("T4", "(6, 10)", "Xử lý từ chối: Chấp nhận", VaiNguoiGiao, "(97, null)", false)
        {
            GhiChu = "§2.6 — điểm cuối Đã thu hồi."
        },
        new ChuyenTrangThai("T5", "(6, 10)", "Xử lý từ chối: Bác bỏ", VaiNguoiGiao, "(2, 12)", false)
        {
            GhiChu = "§2.6 — cặp (6, 12) KHÔNG phải điểm cuối; người thực hiện làm lại."
        },
        new ChuyenTrangThai("T6", "(2 | 3 | 7, null | 12)", "Cập nhật tiến độ", VaiNguoiThucHien,
            "không đổi trạng thái", false)
        {
            GhiChu = "§10.2 — validate mucdoht 0-100 là yêu cầu MỚI. §2.4 ghi (2|7) nhưng §6.2 dòng 11 cho cả mã 3."
        },
        new ChuyenTrangThai("T7", "(2 | 3 | 7, null | 12)", "Gửi báo cáo kết quả", VaiNguoiThucHien,
            "(giá trị đã chọn, 10)", false)
        {
            GhiChu = "§5.4 D4 — server validate lại giá trị theo bộ lọc hạn (§1.2 bước 5)."
        },
        new ChuyenTrangThai("T8", "(1 | 5, 10)", "Thu hồi báo cáo", VaiNguoiThucHien,
            "(2 nếu còn hạn / 7 nếu quá hạn, null)", false)
        {
            GhiChu = "§2.6 — cặp (1|5, 10) KHÔNG phải điểm cuối vì còn thu hồi được."
        },
        new ChuyenTrangThai("T9", "(1 | 5, 10)", "Kiểm tra kết quả: ĐẠT", VaiNguoiGiao,
            "(giữ nguyên, 11) → ĐIỂM CUỐI", false)
        {
            GhiChu = "§1.2 bước 7 — mọi hành động tắt, chỉ còn xem chi tiết. Trục A GIỮ NGUYÊN (1 hoặc 5)."
        },
        new ChuyenTrangThai("T10", "(1 | 5, 10)", "Kiểm tra kết quả: CHƯA ĐẠT", VaiNguoiGiao,
            "(2 nếu còn hạn / 7 nếu quá hạn, 12)", false)
        {
            GhiChu = "§10.7 — bắt buộc nội dung phản hồi là yêu cầu MỚI. Quay lại T6/T7."
        },
        new ChuyenTrangThai("T11", "(2 | 3 | 7, *), giahan ≠ 10, solangiahan < 2", "Xin gia hạn", VaiNguoiThucHien,
            "trangthai := 13, trangthaixulygiahan := 10", false)
        {
            GhiChu = "§2.3 — ràng buộc còn dưới 3 ngày của hệ gốc là mã chết, KHÔNG áp dụng."
        },
        new ChuyenTrangThai("T12", "trangthaixulygiahan = 10", "Duyệt gia hạn", VaiNguoiGiao,
            "trangthaixulygiahan := 11, solangiahan += 1, cập nhật hanxulyth", false)
        {
            GhiChu = "§5.6 — hệ gốc KHÔNG có logic cập nhật hanxulyth ở FE; đây là đặc tả MỚI. Chặn khi trangthai = 97."
        },
        new ChuyenTrangThai("T13", "trangthaixulygiahan = 10", "Từ chối gia hạn", VaiNguoiGiao,
            "trangthaixulygiahan := 12", false)
        {
            GhiChu = "§5.6 — trục A trở về giá trị trước khi xin gia hạn (2 hoặc 7 tuỳ hạn; giữ 3 nếu chưa tiếp nhận)."
        },
        new ChuyenTrangThai("T14", "trangthai ∉ {1, 5, 97}", "Thu hồi nhiệm vụ", VaiNguoiGiao,
            "(97, null) → ĐIỂM CUỐI", false)
        {
            GhiChu = "§1.3 — không có đường quay lại. Phải đóng mọi đề xuất gia hạn còn treo."
        }
    };

    /// <summary>Toan bo 14 dong T1..T14 cua §2.4, dung thu tu dac ta.</summary>
    public static IReadOnlyList<ChuyenTrangThai> ToanBo => _toanBo;

    /// <summary>Tim mot dong theo ma "T1".."T14". Tra <c>null</c> neu khong co.</summary>
    public static ChuyenTrangThai? Tim(string? ma) =>
        ma is null
            ? null
            : _toanBo.FirstOrDefault(x => string.Equals(x.Ma, ma, StringComparison.OrdinalIgnoreCase));

    /// <summary>§1 — so do 7 buoc cua quy trinh dich.</summary>
    public static IReadOnlyList<BuocQuyTrinh> CacBuoc { get; } = new[]
    {
        new BuocQuyTrinh(1, "Tạo nhiệm vụ"),
        new BuocQuyTrinh(2, "Giao nhiệm vụ"),
        new BuocQuyTrinh(3, "Tiếp nhận"),
        new BuocQuyTrinh(4, "Thực hiện & cập nhật tiến độ"),
        new BuocQuyTrinh(5, "Gửi báo cáo kết quả"),
        new BuocQuyTrinh(6, "Kiểm tra kết quả"),
        new BuocQuyTrinh(7, "Hoàn thành")
    };

    /// <summary>
    /// §1 — nhiem vu dang o buoc nao trong so do 7 buoc.
    /// Ham THUAN TUY, chi doc hai truc trang thai. Luon tra gia tri trong khoang 1..7.
    /// </summary>
    /// <param name="nv">Nhiem vu can xet; <c>null</c> = chua co ban ghi =&gt; dang o buoc 1 (Tao).</param>
    public static int BuocSoDo(DmNhiemVuChiTiet? nv)
    {
        if (nv is null) return 1;                                     // chua co ban ghi => dang tao

        int tt = nv.TrangThai;
        int? dv = nv.TrangThaiDvXuly;

        // §2.6 — diem cuoi "Hoan thanh - da nghiem thu".
        if (TrangThaiNv.DaHoanThanh.Contains(tt) && dv == TrangThaiPh.DaXacNhan) return 7;
        // §2.4 T4/T14 — da thu hoi: dung lai o khau Giao.
        if (tt == TrangThaiNv.DaThuHoi) return 2;
        // §2.4 T3 — tu choi: van thuoc khau Tiep nhan.
        if (tt == TrangThaiNv.TuChoi) return 3;
        // §1.2 buoc 6 — da gui bao cao, dang cho kiem tra.
        if (dv == TrangThaiPh.ChoXacNhan) return 6;
        // §1.2 buoc 3 — da giao, cho tiep nhan.
        if (tt == TrangThaiNv.ChuaTrienKhai) return 3;
        // §1.2 buoc 4 — dang thuc hien (ke ca dang xin gia han, ma 13).
        if (tt is TrangThaiNv.DangTrienKhai or TrangThaiNv.DangTrienKhaiQuaHan or TrangThaiNv.GiaHan) return 4;
        // (1|5, null|12) — da bao cao nhung bi thu hoi / bi tra lai => quay lai khau Bao cao.
        if (TrangThaiNv.DaHoanThanh.Contains(tt)) return 5;
        return 2;
    }

    /// <summary>
    /// §6.2 — mo ta 15 hanh dong (14 dong cua bang + 1 mo rong "Xu ly tu choi").
    /// Ten <c>Ma</c> trung voi ten khoa JSON cua <c>QuyenNhiemVuDto</c>.
    /// </summary>
    public static IReadOnlyList<MoTaHanhDong> HanhDong { get; } = new[]
    {
        new MoTaHanhDong("suaNhiemVu",     "Sửa nhiệm vụ",          6,  VaiNguoiGiao),
        new MoTaHanhDong("thuHoiNhiemVu",  "Thu hồi nhiệm vụ",      7,  VaiNguoiGiao),
        new MoTaHanhDong("thuHoiPhanCong", "Thu hồi phân công",     8,  VaiNguoiGiao),
        new MoTaHanhDong("tiepNhan",       "Tiếp nhận",             9,  VaiNguoiThucHien),
        new MoTaHanhDong("tuChoi",         "Từ chối nhiệm vụ",      10, VaiNguoiThucHien),
        new MoTaHanhDong("capNhatTienDo",  "Cập nhật tiến độ",      11, VaiNguoiThucHien),
        new MoTaHanhDong("guiBaoCao",      "Gửi báo cáo kết quả",   12, VaiNguoiThucHien),
        new MoTaHanhDong("thuHoiBaoCao",   "Thu hồi báo cáo",       13, VaiNguoiThucHien),
        new MoTaHanhDong("kiemTraKetQua",  "Kiểm tra kết quả",      14, VaiNguoiGiao),
        new MoTaHanhDong("xinGiaHan",      "Xin gia hạn",           15, VaiNguoiThucHien),
        new MoTaHanhDong("duyetGiaHan",    "Duyệt gia hạn",         16, VaiNguoiGiao),
        new MoTaHanhDong("nhacViec",       "Nhắc việc",             17, VaiNguoiGiao),
        new MoTaHanhDong("xemChiTiet",     "Xem chi tiết",          18, VaiTatCaLienQuan),
        new MoTaHanhDong("taiTep",         "Tải tệp đính kèm",      19, VaiTatCaLienQuan),
        // MO RONG: §6.2 khong co dong nao cho §2.4 T4/T5 => dong = 0.
        new MoTaHanhDong("xuLyTuChoi",     "Xử lý đề nghị từ chối", 0,  VaiNguoiGiao)
    };
}
