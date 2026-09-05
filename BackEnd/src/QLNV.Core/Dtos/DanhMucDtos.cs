using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.1 A4 — mot muc danh muc tra ve tu
/// <c>GET /api/v1/danh-muc?type=TRANGTHAINV,TRANGTHAIPH,LOAIVB,DOKHAN</c>.
/// </summary>
public sealed class TuDienDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Ma dang chuoi: "1", "2", "13", "97" hoac "TRONGTAM"...</summary>
    [JsonPropertyName("ma")]
    public string Ma { get; set; } = string.Empty;

    [JsonPropertyName("nhan")]
    public string Nhan { get; set; } = string.Empty;

    /// <summary>"xam" | "vang" | "xanh" | "do" | "duong" | "tim" | "cam".</summary>
    [JsonPropertyName("mau")]
    public string? Mau { get; set; }

    [JsonPropertyName("mota")]
    public string? MoTa { get; set; }

    [JsonPropertyName("thutu")]
    public int ThuTu { get; set; }
}

/// <summary>§5.1 A5 — mot nut cua cay linh vuc <c>GET /api/v1/linh-vuc</c>.</summary>
public sealed class LinhVucDto
{
    [JsonPropertyName("ma")]
    public string Ma { get; set; } = string.Empty;

    [JsonPropertyName("ten")]
    public string Ten { get; set; } = string.Empty;

    [JsonPropertyName("nhomCha")]
    public string? NhomCha { get; set; }

    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    [JsonPropertyName("thutu")]
    public int ThuTu { get; set; }

    /// <summary>Cac linh vuc con — rong khi la nut la.</summary>
    [JsonPropertyName("con")]
    public List<LinhVucDto> Con { get; set; } = new();
}

/// <summary>§5.1 A6 — mot nut cua cay don vi + nguoi dung <c>GET /api/v1/don-vi/cay</c>.</summary>
public sealed class DonViDto
{
    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("tendonvi")]
    public string TenDonVi { get; set; } = string.Empty;

    [JsonPropertyName("macha")]
    public string? MaCha { get; set; }

    [JsonPropertyName("capdonvi")]
    public int CapDonVi { get; set; }

    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    /// <summary>Nguoi dung truc thuoc don vi nay.</summary>
    [JsonPropertyName("nguoiDung")]
    public List<NguoiDungTomTatDto> NguoiDung { get; set; } = new();

    /// <summary>Cac don vi con.</summary>
    [JsonPropertyName("con")]
    public List<DonViDto> Con { get; set; } = new();
}

/// <summary>
/// Thong tin nguoi dung rut gon — dung trong cay don vi (§5.1 A6),
/// danh sach phan cong (§5.3 C4) va cac o chon nguoi.
/// </summary>
public sealed class NguoiDungTomTatDto
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("chucvu")]
    public string? ChucVu { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("unitname")]
    public string? UnitName { get; set; }

    [JsonPropertyName("vaitro")]
    public string? VaiTro { get; set; }
}
