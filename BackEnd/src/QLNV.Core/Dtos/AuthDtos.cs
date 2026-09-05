using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>§5.1 A1 — body cua <c>POST /api/v1/auth/login</c>.</summary>
public sealed class DangNhapRequest
{
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>§5.1 A2 — body cua <c>POST /api/v1/auth/refresh</c>.</summary>
public sealed class LamMoiTokenRequest
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Cap token do <c>IJwtService</c> phat hanh (§7.2: JWT tu phat hanh, secret doc tu cau hinh).</summary>
public sealed class CapTokenDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("loaiToken")]
    public string LoaiToken { get; set; } = "Bearer";

    /// <summary>Thoi diem het han cua access token.</summary>
    [JsonPropertyName("hetHanLuc")]
    public DateTime HetHanLuc { get; set; }

    /// <summary>Thoi diem het han cua refresh token.</summary>
    [JsonPropertyName("refreshHetHanLuc")]
    public DateTime RefreshHetHanLuc { get; set; }
}

/// <summary>§5.1 A1/A2 — ket qua dang nhap: cap token + thong tin nguoi dung.</summary>
public sealed class DangNhapResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("loaiToken")]
    public string LoaiToken { get; set; } = "Bearer";

    [JsonPropertyName("hetHanLuc")]
    public DateTime HetHanLuc { get; set; }

    [JsonPropertyName("nguoiDung")]
    public NguoiDungDto NguoiDung { get; set; } = new();
}

/// <summary>Thong tin nguoi dang dang nhap, tra kem token (§5.1 A1).</summary>
public sealed class NguoiDungDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("chucvu")]
    public string? ChucVu { get; set; }

    [JsonPropertyName("unitcode")]
    public string UnitCode { get; set; } = string.Empty;

    [JsonPropertyName("unitname")]
    public string? UnitName { get; set; }

    /// <summary>QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN (§6.1).</summary>
    [JsonPropertyName("vaitro")]
    public string VaiTro { get; set; } = string.Empty;

    [JsonPropertyName("trangthai")]
    public int TrangThai { get; set; }

    /// <summary>§4.7 — nguong tai dung cho AI (K trong §9.4 S4).</summary>
    [JsonPropertyName("maxConcurrentTasks")]
    public int MaxConcurrentTasks { get; set; }
}

/// <summary>§5.1 A3 — body cua <c>POST /api/v1/auth/logout</c>.</summary>
public sealed class DangXuatRequest
{
    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }
}

/// <summary>Doi mat khau — phuc vu §5.9 I1.</summary>
public sealed class DoiMatKhauRequest
{
    [JsonPropertyName("matKhauCu")]
    public string MatKhauCu { get; set; } = string.Empty;

    [JsonPropertyName("matKhauMoi")]
    public string MatKhauMoi { get; set; } = string.Empty;
}
