namespace TaskApp.Api.Common;

/// <summary>
/// Sau ma trang thai cua NHIEM VU, tuong ung cot TASKS.STATUS_CODE
/// va khoa chinh cua bang danh muc TASK_STATUS_LOOKUP.
/// <para>
/// Vong doi (muc 7 cua DATABASE_SOURCE_OF_TRUTH):
/// MOI_TAO -> DA_GIAO -> DANG_THUC_HIEN -> CHO_XAC_NHAN -> HOAN_THANH,
/// nhanh chua dat: CHO_XAC_NHAN -> YEU_CAU_BO_SUNG -> DANG_THUC_HIEN.
/// </para>
/// Dung nham voi <see cref="TrangThaiBaoCao"/> la loi nghiep vu: hai co che tach roi.
/// </summary>
public static class TrangThaiNhiemVu
{
    /// <summary>Nhiem vu vua duoc khoi tao, chua phan cong nguoi thuc hien.</summary>
    public const string MoiTao = "MOI_TAO";

    /// <summary>Da phan cong nguoi thuc hien, cho nguoi do tiep nhan.</summary>
    public const string DaGiao = "DA_GIAO";

    /// <summary>Nguoi thuc hien da tiep nhan va dang xu ly.</summary>
    public const string DangThucHien = "DANG_THUC_HIEN";

    /// <summary>Da gui bao cao ket qua, cho nguoi giao kiem tra.</summary>
    public const string ChoXacNhan = "CHO_XAC_NHAN";

    /// <summary>Ket qua chua dat, phai chinh sua va bao cao lai.</summary>
    public const string YeuCauBoSung = "YEU_CAU_BO_SUNG";

    /// <summary>Da duoc nguoi giao xac nhan hoan thanh. Day la trang thai ket thuc.</summary>
    public const string HoanThanh = "HOAN_THANH";

    /// <summary>Toan bo trang thai hop le, theo dung thu tu SORT_ORDER 1..6.</summary>
    public static readonly string[] TatCa =
    {
        MoiTao, DaGiao, DangThucHien, ChoXacNhan, YeuCauBoSung, HoanThanh
    };

    /// <summary>
    /// Cac trang thai duoc coi la "dang xu ly" khi dem khoi luong cong viec cua mot nhan su.
    /// Dung cho chuc nang AI goi y nguoi thuc hien (muc 8 cua DATABASE_SOURCE_OF_TRUTH).
    /// </summary>
    public static readonly string[] DangXuLy =
    {
        DaGiao, DangThucHien, ChoXacNhan, YeuCauBoSung
    };

    /// <summary>Bang chuyen trang thai hop le: tu mot trang thai duoc di toi nhung trang thai nao.</summary>
    private static readonly Dictionary<string, string[]> BangChuyen =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [MoiTao] = new[] { DaGiao },
            [DaGiao] = new[] { DangThucHien },
            [DangThucHien] = new[] { ChoXacNhan },
            [ChoXacNhan] = new[] { HoanThanh, YeuCauBoSung },
            [YeuCauBoSung] = new[] { DangThucHien },
            [HoanThanh] = Array.Empty<string>()
        };

    /// <summary>Ten hien thi tieng Viet cua tung trang thai.</summary>
    private static readonly Dictionary<string, string> BangTen =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [MoiTao] = "Mới tạo",
            [DaGiao] = "Đã giao",
            [DangThucHien] = "Đang thực hiện",
            [ChoXacNhan] = "Chờ xác nhận",
            [YeuCauBoSung] = "Yêu cầu bổ sung",
            [HoanThanh] = "Hoàn thành"
        };

    /// <summary>Kiem tra mot chuoi co phai ma trang thai hop le hay khong.</summary>
    /// <param name="ma">Ma trang thai can kiem tra.</param>
    /// <returns><c>true</c> neu nam trong 6 ma da dinh nghia.</returns>
    public static bool HopLe(string? ma)
    {
        return !string.IsNullOrWhiteSpace(ma) && BangChuyen.ContainsKey(ma);
    }

    /// <summary>
    /// Kiem tra buoc chuyen trang thai co dung vong doi nghiep vu hay khong.
    /// </summary>
    /// <param name="tu">Trang thai hien tai.</param>
    /// <param name="den">Trang thai muon chuyen sang.</param>
    /// <returns>
    /// <c>true</c> neu buoc chuyen hop le. Tra <c>false</c> khi mot trong hai ma khong hop le,
    /// khi giu nguyen trang thai, hoac khi buoc chuyen khong nam trong vong doi.
    /// </returns>
    public static bool ChuyenDuoc(string? tu, string? den)
    {
        if (string.IsNullOrWhiteSpace(tu) || string.IsNullOrWhiteSpace(den))
        {
            return false;
        }

        if (!BangChuyen.TryGetValue(tu, out var danhSachDich))
        {
            return false;
        }

        return danhSachDich.Contains(den, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Liet ke cac trang thai hop le co the chuyen toi tu trang thai dang cho.</summary>
    /// <param name="tu">Trang thai hien tai.</param>
    /// <returns>Mang cac ma trang thai ke tiep; mang rong neu <paramref name="tu"/> khong hop le.</returns>
    public static string[] CacTrangThaiKeTiep(string? tu)
    {
        if (!string.IsNullOrWhiteSpace(tu) && BangChuyen.TryGetValue(tu, out var danhSachDich))
        {
            return danhSachDich;
        }

        return Array.Empty<string>();
    }

    /// <summary>Tra ve ten hien thi tieng Viet cua trang thai.</summary>
    /// <param name="ma">Ma trang thai.</param>
    /// <returns>Ten hien thi, hoac chinh chuoi dau vao neu khong nhan ra.</returns>
    public static string TenHienThi(string? ma)
    {
        if (!string.IsNullOrWhiteSpace(ma) && BangTen.TryGetValue(ma, out var ten))
        {
            return ten;
        }

        return ma ?? string.Empty;
    }
}
