namespace TaskApp.Api.Dtos;

/// <summary>Yêu cầu đăng nhập.</summary>
public sealed class DangNhapRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Yêu cầu làm mới cặp token.</summary>
public sealed class LamMoiTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Yêu cầu đăng xuất — thu hồi refresh token đang giữ.</summary>
public sealed class DangXuatRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Yêu cầu đổi mật khẩu của chính người đang đăng nhập.</summary>
public sealed class DoiMatKhauRequest
{
    public string MatKhauCu { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin người dùng trả về cho client.
/// KHÔNG bao giờ chứa <c>PASSWORD_HASH</c>.
/// </summary>
public sealed class NguoiDungDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Vai trò hệ thống: DIRECTOR, DEPT_HEAD, TEAM_LEAD, EMPLOYEE. Cột USER_ROLE.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Tên vai trò để hiển thị, ví dụ "Trưởng nhóm".</summary>
    public string TenVaiTro { get; set; } = string.Empty;

    /// <summary>ACTIVE hoặc INACTIVE. Cột USER_STATUS.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Chức danh trong tổ chức, ví dụ "Lập trình viên Backend". Chỉ để hiển thị.</summary>
    public string? JobTitle { get; set; }

    public long? DepartmentId { get; set; }
    public string? TenPhongBan { get; set; }

    public long? TeamId { get; set; }
    public string? TenNhom { get; set; }
}

/// <summary>Kết quả đăng nhập hoặc làm mới token.</summary>
public sealed class DangNhapResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string LoaiToken { get; set; } = "Bearer";

    /// <summary>Thời điểm access token hết hạn.</summary>
    public DateTime HetHanLuc { get; set; }

    public NguoiDungDto NguoiDung { get; set; } = new();
}
