using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>§5.9 I1 — tham so loc danh sach nguoi dung (M11).</summary>
public sealed class NguoiDungLocRequest
{
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("unitcode")]
    public string? UnitCode { get; set; }

    [JsonPropertyName("vaitro")]
    public string? VaiTro { get; set; }

    [JsonPropertyName("trangthai")]
    public int? TrangThai { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("size")]
    public int Size { get; set; } = Constants.GioiHan.KichThuocTrangMacDinh;
}

/// <summary>§5.9 I1 — body tao / cap nhat nguoi dung.</summary>
public sealed class LuuNguoiDungRequest
{
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Chi bat buoc khi TAO MOI. §10.11: khong bao gio ghi lai dang ro.</summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("chucvu")]
    public string? ChucVu { get; set; }

    /// <summary>QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN.</summary>
    [JsonPropertyName("vaitro")]
    public string VaiTro { get; set; } = string.Empty;

    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; } = 1;

    /// <summary>§4.7 — nguong tai K cho AI, mac dinh 8.</summary>
    [JsonPropertyName("maxConcurrentTasks")]
    public int MaxConcurrentTasks { get; set; } = Constants.GioiHan.MaxConcurrentTasksMacDinh;
}

/// <summary>
/// §5.9 I2 — <c>GET /api/v1/nguoi-dung/{id}/nang-luc</c>.
/// CHI DE XEM. Suy tu lich su (§9.2 / §10.1), KHONG nhap tay.
/// </summary>
public sealed class NangLucNguoiDungDto
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>So nhiem vu da nghiem thu theo tung linh vuc, sap xep giam dan.</summary>
    [JsonPropertyName("theoLinhVuc")]
    public List<NangLucLinhVucDto> TheoLinhVuc { get; set; } = new();

    [JsonPropertyName("tongSoHoanThanh")]
    public int TongSoHoanThanh { get; set; }
}

/// <summary>§5.9 I2 — nang luc suy dien tren mot linh vuc.</summary>
public sealed class NangLucLinhVucDto
{
    [JsonPropertyName("linhvuc")]
    public string LinhVuc { get; set; } = string.Empty;

    [JsonPropertyName("tenLinhVuc")]
    public string? TenLinhVuc { get; set; }

    /// <summary>§9.4 S1 — so nhiem vu da nghiem thu thuoc linh vuc nay.</summary>
    [JsonPropertyName("soNvHoanThanh")]
    public int SoNvHoanThanh { get; set; }

    /// <summary>Diem S1 tinh duoc: <c>min(1, n / nguongChuyenMon)</c>.</summary>
    [JsonPropertyName("diemChuyenMon")]
    public double DiemChuyenMon { get; set; }
}

/// <summary>
/// §5.9 I3 — <c>GET /api/v1/nguoi-dung/{id}/hieu-suat</c>.
/// Anh xa 1-1 voi bang tong hop <c>USER_HIEUSUAT</c> (§4.8) va cac dac trung §9.4.
/// </summary>
public sealed class HieuSuatNguoiDungDto
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Null = tong hop moi linh vuc.</summary>
    [JsonPropertyName("linhvuc")]
    public string? LinhVuc { get; set; }

    /// <summary>§4.8 <c>so_nv_hoanthanh</c> — trangthai thuoc {1,5} VA trangthaiDvXuly = 11.</summary>
    [JsonPropertyName("soNvHoanThanh")]
    public int SoNvHoanThanh { get; set; }

    /// <summary>§4.8 <c>so_nv_dunghan</c> — dem rieng trangthai = 1.</summary>
    [JsonPropertyName("soNvDungHan")]
    public int SoNvDungHan { get; set; }

    /// <summary>§4.8 <c>so_nv_bi_tralai</c> — XULY_NHIEMVU co trangthaiDvXuly = 12.</summary>
    [JsonPropertyName("soNvBiTraLai")]
    public int SoNvBiTraLai { get; set; }

    /// <summary>§4.8 <c>so_lan_giahan</c> — tong <c>solangiahan</c>.</summary>
    [JsonPropertyName("soLanGiaHan")]
    public int SoLanGiaHan { get; set; }

    /// <summary>§4.8 view <c>so_nv_dang_mo</c>.</summary>
    [JsonPropertyName("soNvDangMo")]
    public int SoNvDangMo { get; set; }

    /// <summary>§4.8 view <c>tai_trong_so</c>.</summary>
    [JsonPropertyName("taiTrongSo")]
    public double TaiTrongSo { get; set; }

    /// <summary>§4.8 view <c>so_nv_qua_han</c>.</summary>
    [JsonPropertyName("soNvQuaHan")]
    public int SoNvQuaHan { get; set; }

    [JsonPropertyName("tyLeDungHan")]
    public double TyLeDungHan { get; set; }

    /// <summary>§4.7 <c>max_concurrent_tasks</c>.</summary>
    [JsonPropertyName("K")]
    public int K { get; set; }

    [JsonPropertyName("updatedate")]
    public DateTime? UpdateDate { get; set; }
}

/// <summary>§5.9 I4/I5 — ket qua chay job nen thu cong.</summary>
public sealed class KetQuaJobDto
{
    /// <summary>"CAP_NHAT_HIEU_SUAT" | "CAP_NHAT_QUA_HAN".</summary>
    [JsonPropertyName("job")]
    public string Job { get; set; } = string.Empty;

    /// <summary>So ban ghi bi tac dong.</summary>
    [JsonPropertyName("soBanGhi")]
    public int SoBanGhi { get; set; }

    [JsonPropertyName("batDau")]
    public DateTime BatDau { get; set; }

    [JsonPropertyName("ketThuc")]
    public DateTime KetThuc { get; set; }

    [JsonPropertyName("thongBao")]
    public string? ThongBao { get; set; }
}
