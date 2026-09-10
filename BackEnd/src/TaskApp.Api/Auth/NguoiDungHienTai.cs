using System.Security.Claims;
using TaskApp.Api.Common;

namespace TaskApp.Api.Auth;

/// <summary>
/// Đọc thông tin người đang đăng nhập từ claim của token.
///
/// Mọi kiểm quyền ở backend đều lấy danh tính từ đây, KHÔNG lấy từ thân yêu cầu.
/// Nếu tin userId do client gửi lên thì bất kỳ ai cũng có thể mạo danh người khác.
/// </summary>
public sealed class NguoiDungHienTai
{
    private readonly IHttpContextAccessor _http;

    public NguoiDungHienTai(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    /// <summary>Đã đăng nhập hay chưa.</summary>
    public bool DaDangNhap => Principal?.Identity?.IsAuthenticated == true;

    /// <summary>Id người dùng, null nếu chưa đăng nhập.</summary>
    public long? UserId
    {
        get
        {
            var s = Principal?.FindFirst(TenClaim.UserId)?.Value;
            return long.TryParse(s, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirst(TenClaim.Username)?.Value;

    public string? FullName => Principal?.FindFirst(TenClaim.FullName)?.Value;

    /// <summary>Vai trò: MANAGER hoặc EMPLOYEE.</summary>
    public string? VaiTroHienTai => Principal?.FindFirst(ClaimTypes.Role)?.Value;

    /// <summary>Là người giao nhiệm vụ.</summary>
    public bool LaManager => VaiTroHienTai == VaiTro.Manager;

    /// <summary>Là người thực hiện nhiệm vụ.</summary>
    public bool LaEmployee => VaiTroHienTai == VaiTro.Employee;

    /// <summary>
    /// Lấy id người dùng, ném lỗi nếu chưa đăng nhập.
    /// Dùng trong các action đã gắn [Authorize] — khi đó luôn phải có id.
    /// </summary>
    public long LayUserIdBatBuoc()
        => UserId ?? throw new UnauthorizedAccessException("Chưa đăng nhập.");
}
