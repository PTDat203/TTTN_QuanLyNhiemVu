namespace QLNV.Core.Entities;

/// <summary>
/// Bang <c>DM_TUDIEN</c> — danh muc ma -&gt; nhan (§5.1 A4).
///
/// §10.5 / §2 ghi chu: he goc KHONG co bang ma-&gt;nhan cho TRANGTHAINV / TRANGTHAIPH
/// duoi bat ky dang nao trong ma nguon FE; nhan nap luc chay tu may chu.
/// App moi BAT BUOC seed bang nay voi 4 type cua <see cref="Constants.MaTypeTuDien"/>.
///
/// Rang buoc: cap (<see cref="Type"/>, <see cref="Ma"/>) la duy nhat.
/// </summary>
public class DmTuDien
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ma type: TRANGTHAINV / TRANGTHAIPH / LOAIVB / DOKHAN
    /// (xem <see cref="Constants.MaTypeTuDien"/>). Cot <c>type</c>, varchar(50).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Ma gia tri, luu dang CHUOI cho ca hai loai: truc so ("1", "2", "13", "97")
    /// va truc chuoi ("TRONGTAM"). Cot <c>ma</c>, varchar(50).
    /// </summary>
    public string Ma { get; set; } = string.Empty;

    /// <summary>Nhan hien thi tieng Viet co dau. Cot <c>nhan</c>.</summary>
    public string Nhan { get; set; } = string.Empty;

    /// <summary>
    /// Ma mau ngu nghia dang chuoi de FE tu anh xa:
    /// "xam" | "vang" | "xanh" | "do" | "duong" | "tim" | "cam". Cot <c>mau</c>.
    /// </summary>
    public string? Mau { get; set; }

    /// <summary>Mo ta y nghia. Cot <c>mota</c>.</summary>
    public string? MoTa { get; set; }

    /// <summary>Thu tu hien thi trong danh sach. Cot <c>thutu</c>.</summary>
    public int ThuTu { get; set; }

    /// <summary>1 = dang dung, 0 = ngung. Cot <c>trangthai</c>.</summary>
    public int TrangThai { get; set; } = 1;
}
