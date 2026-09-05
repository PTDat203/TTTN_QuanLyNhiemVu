namespace QLNV.Core.Constants;

/// <summary>
/// §4.7 / §6.1 — MOT truc vai tro duy nhat cua app nho (<c>SYS_USER.vaitro</c>).
/// He goc dung 3 truc chong cheo (Level / Chucvu / roles JWT) — khong bo sang.
/// </summary>
public static class VaiTro
{
    /// <summary>Quan tri he thong — §6.2 cot QUAN_TRI.</summary>
    public const string QuanTri = "QUAN_TRI";

    /// <summary>Nguoi giao nhiem vu — §6.2 cot NGUOI_GIAO.</summary>
    public const string NguoiGiao = "NGUOI_GIAO";

    /// <summary>Nguoi thuc hien nhiem vu — §6.2 cot NGUOI_THUC_HIEN.</summary>
    public const string NguoiThucHien = "NGUOI_THUC_HIEN";

    public static IReadOnlyList<string> ToanBo() => new[] { QuanTri, NguoiGiao, NguoiThucHien };

    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);

    /// <summary>
    /// §6.4 lop 1 — nhom duoc goi cac endpoint cua nguoi giao
    /// (§6.2 dong 6, 7, 8, 14, 16, 17): QUAN_TRI hoac NGUOI_GIAO.
    /// </summary>
    public static bool LaBenGiao(string? vaiTro) => vaiTro is QuanTri or NguoiGiao;

    /// <summary>
    /// §6.4 lop 1 — nhom duoc goi cac endpoint cua nguoi thuc hien
    /// (§6.2 dong 9, 10, 11, 12, 13, 15): CHI NGUOI_THUC_HIEN.
    /// </summary>
    public static bool LaBenLam(string? vaiTro) => vaiTro == NguoiThucHien;

    public static string Nhan(string? ma) => ma switch
    {
        QuanTri => "Quản trị hệ thống",
        NguoiGiao => "Người giao nhiệm vụ",
        NguoiThucHien => "Người thực hiện",
        _ => "Không xác định"
    };
}
