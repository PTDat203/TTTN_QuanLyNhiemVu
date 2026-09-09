namespace TaskApp.Api.Common;

/// <summary>
/// Ba muc uu tien cua nhiem vu, tuong ung cot TASKS.PRIORITY
/// va rang buoc CK_TASK_PRIORITY. Luu trong database bang tieng Anh viet hoa,
/// giao dien tu quy doi sang Thap / Trung binh / Cao.
/// </summary>
public static class MucUuTien
{
    /// <summary>Uu tien thap.</summary>
    public const string Thap = "LOW";

    /// <summary>Uu tien trung binh - gia tri mac dinh trong DDL.</summary>
    public const string TrungBinh = "MEDIUM";

    /// <summary>Uu tien cao.</summary>
    public const string Cao = "HIGH";

    /// <summary>Toan bo muc uu tien hop le, xep tu thap den cao.</summary>
    public static readonly string[] TatCa = { Thap, TrungBinh, Cao };

    /// <summary>Kiem tra mot chuoi co phai muc uu tien hop le hay khong.</summary>
    /// <param name="ma">Ma muc uu tien can kiem tra.</param>
    /// <returns><c>true</c> neu la LOW, MEDIUM hoac HIGH.</returns>
    public static bool HopLe(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma))
        {
            return false;
        }

        return TatCa.Contains(ma, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Quy doi muc uu tien sang so de sap xep: LOW = 1, MEDIUM = 2, HIGH = 3.
    /// </summary>
    /// <param name="ma">Ma muc uu tien.</param>
    /// <returns>Trong so tu 1 den 3; tra 0 neu khong nhan ra.</returns>
    public static int TrongSo(string? ma)
    {
        if (string.Equals(ma, Thap, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(ma, TrungBinh, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(ma, Cao, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 0;
    }

    /// <summary>Tra ve ten hien thi tieng Viet cua muc uu tien.</summary>
    /// <param name="ma">Ma muc uu tien.</param>
    /// <returns>Ten hien thi, hoac chinh chuoi dau vao neu khong nhan ra.</returns>
    public static string TenHienThi(string? ma)
    {
        return TrongSo(ma) switch
        {
            1 => "Thấp",
            2 => "Trung bình",
            3 => "Cao",
            _ => ma ?? string.Empty
        };
    }
}
