namespace TaskApp.Api.Common;

/// <summary>
/// Hai vai tro nguoi dung, tuong ung cot USERS.ROLE va rang buoc CK_USERS_ROLE.
/// Cac hang so nay cung duoc dung lam ten role trong JWT
/// (<c>[Authorize(Roles = VaiTro.Manager)]</c>).
/// </summary>
public static class VaiTro
{
    /// <summary>Nguoi giao nhiem vu: tao, giao, duyet bao cao.</summary>
    public const string Manager = "MANAGER";

    /// <summary>Nguoi thuc hien nhiem vu: tiep nhan, cap nhat tien do, gui bao cao.</summary>
    public const string Employee = "EMPLOYEE";

    /// <summary>Toan bo vai tro hop le.</summary>
    public static readonly string[] TatCa = { Manager, Employee };

    /// <summary>Kiem tra mot chuoi co phai vai tro hop le hay khong.</summary>
    /// <param name="vaiTro">Chuoi can kiem tra.</param>
    /// <returns><c>true</c> neu la MANAGER hoac EMPLOYEE.</returns>
    public static bool HopLe(string? vaiTro)
    {
        if (string.IsNullOrWhiteSpace(vaiTro))
        {
            return false;
        }

        return TatCa.Contains(vaiTro, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Tra ve ten hien thi tieng Viet cua vai tro.</summary>
    /// <param name="vaiTro">Ma vai tro.</param>
    /// <returns>Ten hien thi, hoac chinh chuoi dau vao neu khong nhan ra.</returns>
    public static string TenHienThi(string? vaiTro)
    {
        if (string.Equals(vaiTro, Manager, StringComparison.OrdinalIgnoreCase))
        {
            return "Người giao nhiệm vụ";
        }

        if (string.Equals(vaiTro, Employee, StringComparison.OrdinalIgnoreCase))
        {
            return "Người thực hiện";
        }

        return vaiTro ?? string.Empty;
    }
}
