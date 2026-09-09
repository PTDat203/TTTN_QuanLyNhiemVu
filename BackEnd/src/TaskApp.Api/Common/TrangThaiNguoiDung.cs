namespace TaskApp.Api.Common;

/// <summary>
/// Trang thai tai khoan nguoi dung, tuong ung cot USERS.STATUS
/// va rang buoc CK_USERS_STATUS.
/// </summary>
public static class TrangThaiNguoiDung
{
    /// <summary>Tai khoan dang hoat dong, duoc phep dang nhap.</summary>
    public const string HoatDong = "ACTIVE";

    /// <summary>Tai khoan bi khoa, khong duoc phep dang nhap va khong duoc giao nhiem vu moi.</summary>
    public const string NgungHoatDong = "INACTIVE";

    /// <summary>Toan bo trang thai hop le.</summary>
    public static readonly string[] TatCa = { HoatDong, NgungHoatDong };

    /// <summary>Kiem tra mot chuoi co phai trang thai tai khoan hop le hay khong.</summary>
    /// <param name="ma">Ma trang thai can kiem tra.</param>
    /// <returns><c>true</c> neu la ACTIVE hoac INACTIVE.</returns>
    public static bool HopLe(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma))
        {
            return false;
        }

        return TatCa.Contains(ma, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Tra ve ten hien thi tieng Viet cua trang thai tai khoan.</summary>
    /// <param name="ma">Ma trang thai.</param>
    /// <returns>Ten hien thi, hoac chinh chuoi dau vao neu khong nhan ra.</returns>
    public static string TenHienThi(string? ma)
    {
        if (string.Equals(ma, HoatDong, StringComparison.OrdinalIgnoreCase))
        {
            return "Đang hoạt động";
        }

        if (string.Equals(ma, NgungHoatDong, StringComparison.OrdinalIgnoreCase))
        {
            return "Ngừng hoạt động";
        }

        return ma ?? string.Empty;
    }
}
