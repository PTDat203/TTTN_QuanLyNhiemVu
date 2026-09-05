namespace QLNV.Core.Constants;

/// <summary>
/// §4.3 — cot <c>vaitro</c> cua bang NHIEMVU_PHANCONG.
/// Cai tien so voi he goc (goc phan biet bang mang listUserTh / listUserPh).
/// Rang buoc §4.3: moi nhiem vu phai co it nhat 1 CHUTRI con hieu luc; mot userid
/// KHONG duoc vua CHUTRI vua PHOIHOP tren cung 1 nhiem vu.
/// </summary>
public static class VaiTroPhanCong
{
    /// <summary>Chu tri — chiu trach nhiem chinh, la vai duy nhat co luong trang thai (§6.3).</summary>
    public const string ChuTri = "CHUTRI";

    /// <summary>Phoi hop — §6.3: CHI xem chi tiet, xem lich su, tai tep. Khong co luong trang thai rieng.</summary>
    public const string PhoiHop = "PHOIHOP";

    public static IReadOnlyList<string> ToanBo() => new[] { ChuTri, PhoiHop };

    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);

    public static string Nhan(string? ma) => ma switch
    {
        ChuTri => "Chủ trì",
        PhoiHop => "Phối hợp",
        _ => "Không xác định"
    };
}

/// <summary>§4.3 — cot <c>trangthai</c> cua bang NHIEMVU_PHANCONG.</summary>
public static class TrangThaiPhanCong
{
    /// <summary>1 = con hieu luc.</summary>
    public const int ConHieuLuc = 1;

    /// <summary>0 = da thu hoi phan cong (§5.3 C7).</summary>
    public const int DaThuHoi = 0;
}
