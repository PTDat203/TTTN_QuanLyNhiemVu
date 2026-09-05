namespace QLNV.Core.Constants;

/// <summary>
/// §7.4 muc 4 + §4.1/§4.2 truong <c>dokhan</c> — muc do uu tien.
/// Bam hang MUCDOUUTIEN cua he goc. Trong so tai dung cho §9.4 S4 va view
/// USER_TAI_HIENTAI (§4.8).
/// </summary>
public static class DoKhan
{
    /// <summary>Nhiem vu trong tam — he so tai 1,5 (§4.8 / §9.4 S4).</summary>
    public const string TrongTam = "TRONGTAM";

    /// <summary>Cong viec thuong xuyen — he so tai 1,0 (§4.8 / §9.4 S4).</summary>
    public const string ThuongXuyen = "THUONGXUYEN";

    /// <summary>Nhiem vu dot xuat — he so tai 2,0 (§4.8 / §9.4 S4).</summary>
    public const string DotXuat = "DOTXUAT";

    /// <summary>Toan bo ma do khan hop le, thu tu uu tien giam dan.</summary>
    public static IReadOnlyList<string> ToanBo() => new[] { DotXuat, TrongTam, ThuongXuyen };

    /// <summary>Kiem tra ma do khan co hop le khong (phan biet chu HOA/thuong).</summary>
    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);

    /// <summary>
    /// §9.4 S4 / §4.8 — trong so tai theo do khan.
    /// DOTXUAT = 2,0 · TRONGTAM = 1,5 · THUONGXUYEN = 1,0.
    /// Gia tri khong xac dinh tra 1,0 (dung nhanh ELSE cua view USER_TAI_HIENTAI).
    /// </summary>
    public static double TrongSo(string? ma) => ma switch
    {
        DotXuat => 2.0,
        TrongTam => 1.5,
        ThuongXuyen => 1.0,
        _ => 1.0
    };

    /// <summary>Nhan tieng Viet mac dinh (§10.5 — FE nen lay tu DM_TUDIEN type DOKHAN).</summary>
    public static string Nhan(string? ma) => ma switch
    {
        DotXuat => "Đột xuất",
        TrongTam => "Trọng tâm",
        ThuongXuyen => "Thường xuyên",
        _ => "Không xác định"
    };
}
