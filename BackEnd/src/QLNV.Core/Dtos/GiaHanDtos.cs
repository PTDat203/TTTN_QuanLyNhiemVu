using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.6 F1 — body <c>POST /api/v1/nhiem-vu/{id}/gia-han</c> (§2.4 T11).
/// Ket qua: <c>trangthai := 13</c>, <c>trangthaixulygiahan := 10</c>.
/// Chan khi <c>solangiahan &gt;= 2</c> hoac da co yeu cau treo (§1.3, §6.2 dong 15).
/// </summary>
public sealed class GiaHanRequest
{
    /// <summary>
    /// BAT BUOC. Phai MUON HON <c>hanxulyth</c> hien tai (§3.3 M10: <c>min</c> = han hien tai).
    /// </summary>
    [JsonPropertyName("hanxulydexuat")]
    public DateOnly HanXuLyDeXuat { get; set; }

    /// <summary>Ly do gia han. Khong bat buoc, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("noiDung")]
    public string? NoiDung { get; set; }

    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}

/// <summary>
/// §5.6 F2 — body <c>POST /api/v1/gia-han/{idGiaHan}/duyet</c> (§2.4 T12/T13).
/// DUYET   =&gt; trangthaixulygiahan := 11, solangiahan += 1, hanxulyth := hanxulydexuat.
/// TU_CHOI =&gt; trangthaixulygiahan := 12, han giu nguyen.
/// </summary>
public sealed class DuyetGiaHanRequest
{
    /// <summary>"DUYET" | "TU_CHOI" (xem <c>KetQuaDuyetGiaHan</c>).</summary>
    [JsonPropertyName("ketQua")]
    public string KetQua { get; set; } = string.Empty;

    /// <summary>Y kien nguoi duyet. Khong bat buoc, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("phanHoi")]
    public string? PhanHoi { get; set; }

    /// <summary>
    /// Id ban ghi gia han can duyet. Tuy chon khi endpoint da co <c>{idGiaHan}</c> tren URL;
    /// bat buoc khi goi qua <c>INhiemVuStateMachine</c> ma nhiem vu co nhieu de xuat.
    /// </summary>
    [JsonPropertyName("idGiaHan")]
    public Guid? IdGiaHan { get; set; }
}

/// <summary>§5.6 F3 — mot dong trong lich su gia han (§4.5), toi da 2 lan duoc duyet.</summary>
public sealed class GiaHanDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("idCtnv")]
    public Guid IdCtnv { get; set; }

    [JsonPropertyName("noiDung")]
    public string? NoiDung { get; set; }

    [JsonPropertyName("hanxulydexuat")]
    public DateOnly HanXuLyDeXuat { get; set; }

    [JsonPropertyName("hanxulyth_cu")]
    public DateOnly? HanXuLyThCu { get; set; }

    /// <summary>10 cho / 11 duyet / 12 tu choi (§2.3).</summary>
    [JsonPropertyName("trangThai")]
    public int TrangThai { get; set; }

    [JsonPropertyName("tenTrangThai")]
    public string? TenTrangThai { get; set; }

    [JsonPropertyName("phanhoi")]
    public string? PhanHoi { get; set; }

    [JsonPropertyName("useridDexuat")]
    public Guid UserIdDeXuat { get; set; }

    [JsonPropertyName("nguoiDeXuatTen")]
    public string? NguoiDeXuatTen { get; set; }

    [JsonPropertyName("useridDuyet")]
    public Guid? UserIdDuyet { get; set; }

    [JsonPropertyName("nguoiDuyetTen")]
    public string? NguoiDuyetTen { get; set; }

    [JsonPropertyName("createDate")]
    public DateTime CreateDate { get; set; }

    [JsonPropertyName("ngayduyet")]
    public DateTime? NgayDuyet { get; set; }

    [JsonPropertyName("files")]
    public List<FileDto> Files { get; set; } = new();
}
