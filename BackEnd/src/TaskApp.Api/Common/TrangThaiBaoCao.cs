namespace TaskApp.Api.Common;

/// <summary>
/// Ba trang thai cua BAO CAO, tuong ung cot TASK_REPORTS.STATUS
/// va rang buoc CK_REPORT_STATUS.
/// <para>
/// KHONG dung chung danh muc voi <see cref="TrangThaiNhiemVu"/>. Hai chuoi
/// <c>CHO_XAC_NHAN</c> tuy giong nhau ve chinh ta nhung thuoc hai co che khac nhau:
/// mot ben la trang thai nhiem vu (co bang lookup), mot ben la trang thai bao cao (CHECK).
/// </para>
/// </summary>
public static class TrangThaiBaoCao
{
    /// <summary>Bao cao vua duoc gui, cho nguoi giao kiem tra.</summary>
    public const string ChoXacNhan = "CHO_XAC_NHAN";

    /// <summary>Nguoi giao da xac nhan dat, keo theo nhiem vu chuyen sang HOAN_THANH.</summary>
    public const string DaXacNhan = "DA_XAC_NHAN";

    /// <summary>Nguoi giao tu choi, keo theo nhiem vu chuyen sang YEU_CAU_BO_SUNG.</summary>
    public const string TuChoi = "TU_CHOI";

    /// <summary>Toan bo trang thai bao cao hop le.</summary>
    public static readonly string[] TatCa = { ChoXacNhan, DaXacNhan, TuChoi };

    /// <summary>Kiem tra mot chuoi co phai trang thai bao cao hop le hay khong.</summary>
    /// <param name="ma">Ma trang thai can kiem tra.</param>
    /// <returns><c>true</c> neu nam trong 3 ma da dinh nghia.</returns>
    public static bool HopLe(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma))
        {
            return false;
        }

        return TatCa.Contains(ma, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Tra ve ten hien thi tieng Viet cua trang thai bao cao.</summary>
    /// <param name="ma">Ma trang thai.</param>
    /// <returns>Ten hien thi, hoac chinh chuoi dau vao neu khong nhan ra.</returns>
    public static string TenHienThi(string? ma)
    {
        if (string.Equals(ma, ChoXacNhan, StringComparison.OrdinalIgnoreCase))
        {
            return "Chờ xác nhận";
        }

        if (string.Equals(ma, DaXacNhan, StringComparison.OrdinalIgnoreCase))
        {
            return "Đã xác nhận";
        }

        if (string.Equals(ma, TuChoi, StringComparison.OrdinalIgnoreCase))
        {
            return "Từ chối";
        }

        return ma ?? string.Empty;
    }
}
