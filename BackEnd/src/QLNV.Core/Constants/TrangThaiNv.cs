namespace QLNV.Core.Constants;

/// <summary>
/// §2.1 Truc A — <c>trangthai</c> (trang thai nhiem vu).
/// Ma giu NGUYEN nhu he goc (§7.4 muc 2): 1/2/3/5/6/7/13/97.
/// CHU Y: KHONG co ma 4, 8, 100 — ca ba deu bi BO o §2.1.
/// Nhan hien thi nam trong bang DM_TUDIEN, type <see cref="MaTypeTuDien.TrangThaiNv"/> (§10.5).
/// </summary>
public static class TrangThaiNv
{
    /// <summary>1 — Hoan thanh (da bao cao xong, con trong han). §2.1</summary>
    public const int HoanThanh = 1;

    /// <summary>2 — Dang trien khai (da tiep nhan, dang lam, con han). §2.1</summary>
    public const int DangTrienKhai = 2;

    /// <summary>3 — Chua trien khai. TRANG THAI KHOI TAO do server dat (§1.2 buoc 2, §10.4).</summary>
    public const int ChuaTrienKhai = 3;

    /// <summary>5 — Hoan thanh - Sau han (bao cao xong nhung tre han). §2.1</summary>
    public const int HoanThanhSauHan = 5;

    /// <summary>6 — Tu choi nhiem vu (nguoi nhan tu choi). §2.1 / §2.4 T3</summary>
    public const int TuChoi = 6;

    /// <summary>7 — Dang trien khai - Da het han. §2.1 / §2.5</summary>
    public const int DangTrienKhaiQuaHan = 7;

    /// <summary>13 — Gia han (dang trong quy trinh xin gia han). §2.1 / §2.4 T11</summary>
    public const int GiaHan = 13;

    /// <summary>97 — Da thu hoi. DIEM CUOI (§2.6), khong co duong quay lai (§1.3).</summary>
    public const int DaThuHoi = 97;

    /// <summary>{1, 5} — "da hoan thanh" theo he goc (TRANGTHAI_DA_HOAN_THANH). §2.1</summary>
    public static readonly IReadOnlyList<int> DaHoanThanh = new[] { HoanThanh, HoanThanhSauHan };

    /// <summary>
    /// {2, 3, 7} — nhiem vu "dang mo": duoc cap nhat tien do / bao cao / xin gia han
    /// (§6.2 dong 11, 12, 15) va la tap ma job qua han I5 quet (§2.5).
    /// </summary>
    public static readonly IReadOnlyList<int> DangMo = new[] { DangTrienKhai, ChuaTrienKhai, DangTrienKhaiQuaHan };

    /// <summary>
    /// {1, 5, 97} — cac ma khoa hanh dong cua nguoi giao (§6.2 dong 7, 17).
    /// LUU Y: day KHONG phai dinh nghia "diem cuoi" cua §2.6 — cap (1|5, 10) va (1|5, 12)
    /// van xu ly tiep duoc; diem cuoi phai xet CA hai truc, dung <see cref="LaDiemCuoi"/>.
    /// </summary>
    public static readonly IReadOnlyList<int> KetThuc = new[] { HoanThanh, HoanThanhSauHan, DaThuHoi };

    /// <summary>Toan bo ma hop le cua truc A theo dung thu tu hien thi (§2.1).</summary>
    public static IReadOnlyList<int> ToanBo() => new[]
    {
        HoanThanh, DangTrienKhai, ChuaTrienKhai, HoanThanhSauHan,
        TuChoi, DangTrienKhaiQuaHan, GiaHan, DaThuHoi
    };

    /// <summary>Kiem tra <paramref name="ma"/> co nam trong tap ma hop le cua truc A khong.</summary>
    public static bool HopLe(int? ma) => ma.HasValue && ToanBo().Contains(ma.Value);

    /// <summary>
    /// §2.6 — diem cuoi cua may trang thai.
    /// Tra "HOAN_THANH_NGHIEM_THU" khi (trangthai ∈ {1,5} VA trangthaiDvXuly = 11),
    /// "DA_THU_HOI" khi trangthai = 97, nguoc lai tra <c>null</c>.
    /// KHONG phai diem cuoi: (1|5, 10), (1|5, 12), (6, 12).
    /// </summary>
    public static string? LaDiemCuoi(int? trangThai, int? trangThaiDvXuly)
    {
        if (trangThai.HasValue && DaHoanThanh.Contains(trangThai.Value)
            && trangThaiDvXuly == TrangThaiPh.DaXacNhan)
        {
            return DiemCuoi.HoanThanhNghiemThu;
        }
        if (trangThai == DaThuHoi) return DiemCuoi.DaThuHoi;
        return null;
    }

    /// <summary>
    /// Nhan tieng Viet mac dinh — DUNG cho thong bao loi phia server.
    /// Nhan hien thi cho FE lay tu DM_TUDIEN (§10.5), khong lay tu day.
    /// </summary>
    public static string Nhan(int? ma) => ma switch
    {
        HoanThanh => "Hoàn thành",
        DangTrienKhai => "Đang triển khai",
        ChuaTrienKhai => "Chưa triển khai",
        HoanThanhSauHan => "Hoàn thành - Sau hạn",
        TuChoi => "Từ chối nhiệm vụ",
        DangTrienKhaiQuaHan => "Đang triển khai - Đã hết hạn",
        GiaHan => "Gia hạn",
        DaThuHoi => "Đã thu hồi",
        _ => "Không xác định"
    };
}

/// <summary>§2.6 — hai diem cuoi cua may trang thai.</summary>
public static class DiemCuoi
{
    public const string HoanThanhNghiemThu = "HOAN_THANH_NGHIEM_THU";
    public const string DaThuHoi = "DA_THU_HOI";
}
