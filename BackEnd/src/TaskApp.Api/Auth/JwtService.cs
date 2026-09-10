using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Auth;

/// <summary>Tên các claim dùng trong token.</summary>
public static class TenClaim
{
    public const string UserId = "uid";
    public const string Username = "username";
    public const string FullName = "fullname";

    /// <summary>Vai trò. Dùng tên chuẩn để [Authorize(Roles=...)] hiểu được.</summary>
    public const string VaiTro = ClaimTypes.Role;
}

/// <summary>Cặp token trả về sau khi đăng nhập.</summary>
public sealed record CapToken(string AccessToken, string RefreshToken, DateTime HetHanLuc);

/// <summary>
/// Phát hành và kiểm tra JWT.
///
/// <para>
/// <b>Refresh token lưu trong bộ nhớ tiến trình.</b> Đây là quyết định có chủ đích:
/// lược đồ CSDL đã chốt gồm đúng 7 bảng và không được thêm bảng, nên không có chỗ
/// lưu bền. Hệ quả phải chấp nhận:
/// </para>
/// <list type="bullet">
///   <item>Khởi động lại API thì mọi refresh token mất hiệu lực, người dùng phải đăng nhập lại.</item>
///   <item>Chạy nhiều instance thì token cấp ở instance này không dùng được ở instance kia.</item>
/// </list>
/// <para>
/// Với app một tiến trình phục vụ đồ án thì chấp nhận được. Bản chạy thật cần một bảng
/// REFRESH_TOKENS hoặc một kho ngoài như Redis.
/// </para>
/// </summary>
public sealed class JwtService
{
    private readonly CauHinhJwt _cauHinh;
    private readonly SymmetricSecurityKey _khoa;

    // refreshToken -> (userId, hạn dùng)
    private static readonly ConcurrentDictionary<string, (long UserId, DateTime HetHan)> _kho = new();

    public JwtService(IOptions<CauHinhJwt> cauHinh)
    {
        _cauHinh = cauHinh.Value;
        _cauHinh.KiemTra();
        _khoa = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cauHinh.Key));
    }

    /// <summary>Phát hành access token và refresh token cho một người dùng.</summary>
    public CapToken PhatHanh(User nguoiDung)
    {
        var bayGio = DateTime.UtcNow;
        var hetHan = bayGio.AddMinutes(_cauHinh.SoPhutSongAccessToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, nguoiDung.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TenClaim.UserId, nguoiDung.Id.ToString()),
            new(TenClaim.Username, nguoiDung.Username),
            new(TenClaim.FullName, nguoiDung.FullName),
            new(TenClaim.VaiTro, nguoiDung.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _cauHinh.Issuer,
            audience: _cauHinh.Audience,
            claims: claims,
            notBefore: bayGio,
            expires: hetHan,
            signingCredentials: new SigningCredentials(_khoa, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = TaoRefreshToken();

        _kho[refreshToken] = (nguoiDung.Id, bayGio.AddDays(_cauHinh.SoNgaySongRefreshToken));

        DonTokenHetHan();

        return new CapToken(accessToken, refreshToken, hetHan);
    }

    /// <summary>
    /// Đổi refresh token lấy userId. Trả null nếu token không tồn tại hoặc đã hết hạn.
    /// Token cũ bị thu hồi ngay sau khi dùng (xoay vòng token) — nếu ai đó đánh cắp token
    /// và dùng lại lần hai thì lần đó sẽ hỏng.
    /// </summary>
    public long? DoiRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        if (!_kho.TryRemove(refreshToken, out var muc)) return null;
        if (muc.HetHan < DateTime.UtcNow) return null;
        return muc.UserId;
    }

    /// <summary>Thu hồi một refresh token (đăng xuất).</summary>
    public bool ThuHoi(string refreshToken)
        => !string.IsNullOrWhiteSpace(refreshToken) && _kho.TryRemove(refreshToken, out _);

    /// <summary>Thu hồi mọi refresh token của một người dùng (dùng khi đổi mật khẩu).</summary>
    public int ThuHoiTatCa(long userId)
    {
        var dem = 0;
        foreach (var cap in _kho.Where(x => x.Value.UserId == userId).ToList())
        {
            if (_kho.TryRemove(cap.Key, out _)) dem++;
        }
        return dem;
    }

    /// <summary>Tham số kiểm tra token, dùng chung cho middleware xác thực.</summary>
    public TokenValidationParameters ThamSoKiemTra() => TaoThamSoKiemTra(_cauHinh);

    /// <summary>
    /// Tạo tham số kiểm tra token từ cấu hình. Là hàm static để <c>Program.cs</c> gọi được
    /// lúc đăng ký dịch vụ, khi chưa có thực thể <see cref="JwtService"/> nào.
    /// </summary>
    public static TokenValidationParameters TaoThamSoKiemTra(CauHinhJwt cauHinh) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = cauHinh.Issuer,
        ValidateAudience = true,
        ValidAudience = cauHinh.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cauHinh.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(cauHinh.DoLechDongHoGiay),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = TenClaim.Username
    };

    private static string TaoRefreshToken()
    {
        // 256 bit ngẫu nhiên bằng bộ sinh an toàn mật mã — không dùng Random hay Guid.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Dọn token đã hết hạn để bộ nhớ không phình theo thời gian.</summary>
    private static void DonTokenHetHan()
    {
        var bayGio = DateTime.UtcNow;
        foreach (var cap in _kho.Where(x => x.Value.HetHan < bayGio).ToList())
        {
            _kho.TryRemove(cap.Key, out _);
        }
    }
}
