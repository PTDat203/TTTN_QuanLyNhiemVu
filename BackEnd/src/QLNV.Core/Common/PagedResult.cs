using System.Text.Json.Serialization;

namespace QLNV.Core.Common;

/// <summary>
/// Ket qua phan trang chuan cho cac API danh sach (§5.2 B1, §5.3 C3, §5.9 I1).
/// </summary>
/// <typeparam name="T">Kieu phan tu trong trang.</typeparam>
public sealed class PagedResult<T>
{
    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyList<T> items, int tongSo, int trang, int kichThuoc)
    {
        Items = items;
        TongSo = tongSo;
        Trang = trang;
        KichThuoc = kichThuoc;
    }

    /// <summary>Cac phan tu cua trang hien tai.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>Tong so ban ghi thoa dieu kien loc (khong chi rieng trang nay).</summary>
    [JsonPropertyName("tongSo")]
    public int TongSo { get; set; }

    /// <summary>So hieu trang, bat dau tu 1.</summary>
    [JsonPropertyName("trang")]
    public int Trang { get; set; } = 1;

    /// <summary>So ban ghi moi trang.</summary>
    [JsonPropertyName("kichThuoc")]
    public int KichThuoc { get; set; } = Constants.GioiHan.KichThuocTrangMacDinh;

    /// <summary>Tong so trang, tinh tu <see cref="TongSo"/> va <see cref="KichThuoc"/>.</summary>
    [JsonPropertyName("tongSoTrang")]
    public int TongSoTrang => KichThuoc > 0 ? (int)Math.Ceiling(TongSo / (double)KichThuoc) : 0;

    /// <summary>Tao mot trang rong.</summary>
    public static PagedResult<T> Rong(int trang = 1, int kichThuoc = Constants.GioiHan.KichThuocTrangMacDinh) =>
        new(Array.Empty<T>(), 0, trang, kichThuoc);
}
