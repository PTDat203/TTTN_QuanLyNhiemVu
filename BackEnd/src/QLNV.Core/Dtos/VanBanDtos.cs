using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>§5.2 B1 — tham so loc danh sach van ban chi dao.</summary>
public sealed class VanBanLocRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("size")]
    public int Size { get; set; } = Constants.GioiHan.KichThuocTrangMacDinh;

    /// <summary>Tim theo so ky hieu hoac trich yeu.</summary>
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("tuNgay")]
    public DateOnly? TuNgay { get; set; }

    [JsonPropertyName("denNgay")]
    public DateOnly? DenNgay { get; set; }
}

/// <summary>§5.2 B1 — mot dong trong luoi van ban chi dao (M03).</summary>
public sealed class VanBanDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("sokyhieu")]
    public string? SoKyHieu { get; set; }

    [JsonPropertyName("trichyeu")]
    public string TrichYeu { get; set; } = string.Empty;

    [JsonPropertyName("loaivb")]
    public string? LoaiVb { get; set; }

    [JsonPropertyName("ngaybanhanh")]
    public DateOnly? NgayBanHanh { get; set; }

    [JsonPropertyName("coquanbanhanh")]
    public string? CoQuanBanHanh { get; set; }

    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("thoigianchidao")]
    public DateTime? ThoiGianChiDao { get; set; }

    [JsonPropertyName("nguonnv")]
    public string? NguonNv { get; set; }

    /// <summary>CSV userid lanh dao phu trach (§4.1).</summary>
    [JsonPropertyName("nguoitheodoi")]
    public string? NguoiTheoDoi { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("useridcreate")]
    public Guid UserIdCreate { get; set; }

    [JsonPropertyName("nguoiTaoTen")]
    public string? NguoiTaoTen { get; set; }

    [JsonPropertyName("createdate")]
    public DateTime CreateDate { get; set; }

    /// <summary>§3.2 M03 — cot "So nhiem vu".</summary>
    [JsonPropertyName("tongSoNhiemVu")]
    public int TongSoNhiemVu { get; set; }

    /// <summary>§5.2 B2 — tep dinh kem (chi tra o man chi tiet).</summary>
    [JsonPropertyName("files")]
    public List<FileDto> Files { get; set; } = new();

    /// <summary>§6.2 dong 2/3 — cac quyen cua nguoi dang dang nhap tren ban ghi nay.</summary>
    [JsonPropertyName("choPhepSua")]
    public bool ChoPhepSua { get; set; }

    [JsonPropertyName("choPhepXoa")]
    public bool ChoPhepXoa { get; set; }
}

/// <summary>§5.2 B3/B4 — body tao moi / cap nhat van ban chi dao (M04, 8 truong).</summary>
public sealed class LuuVanBanRequest
{
    [JsonPropertyName("sokyhieu")]
    public string? SoKyHieu { get; set; }

    /// <summary>BAT BUOC, &lt;= 2000 ky tu.</summary>
    [JsonPropertyName("trichyeu")]
    public string TrichYeu { get; set; } = string.Empty;

    [JsonPropertyName("loaivb")]
    public string? LoaiVb { get; set; }

    [JsonPropertyName("ngaybanhanh")]
    public DateOnly? NgayBanHanh { get; set; }

    [JsonPropertyName("coquanbanhanh")]
    public string? CoQuanBanHanh { get; set; }

    /// <summary>BAT BUOC. TRONGTAM / THUONGXUYEN / DOTXUAT.</summary>
    [JsonPropertyName("dokhan")]
    public string DoKhan { get; set; } = string.Empty;

    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    [JsonPropertyName("thoigianchidao")]
    public DateTime? ThoiGianChiDao { get; set; }

    [JsonPropertyName("nguonnv")]
    public string? NguonNv { get; set; }

    [JsonPropertyName("nguoitheodoi")]
    public string? NguoiTheoDoi { get; set; }

    /// <summary>Id cac tep da upload qua §5.7 G1 can gan vao van ban nay.</summary>
    [JsonPropertyName("fileIds")]
    public List<Guid>? FileIds { get; set; }
}
