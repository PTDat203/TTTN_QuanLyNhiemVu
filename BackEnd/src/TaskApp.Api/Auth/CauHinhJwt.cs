namespace TaskApp.Api.Auth;

/// <summary>
/// Cấu hình JWT, đọc từ mục "Jwt" trong appsettings / biến môi trường / user-secrets.
///
/// <para>
/// <b>Key không bao giờ nằm trong mã nguồn hay trong appsettings đã commit.</b>
/// Đặt bằng: <c>dotnet user-secrets set "Jwt:Key" "&lt;chuỗi ngẫu nhiên&gt;"</c>
/// hoặc biến môi trường <c>Jwt__Key</c>.
/// </para>
/// </summary>
public sealed class CauHinhJwt
{
    public const string Muc = "Jwt";

    /// <summary>Khóa ký HMAC-SHA256. Bắt buộc, tối thiểu 32 ký tự.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "TaskApp.Api";
    public string Audience { get; set; } = "TaskApp.Web";

    /// <summary>Thời gian sống của access token, tính bằng phút.</summary>
    public int SoPhutSongAccessToken { get; set; } = 60;

    /// <summary>Thời gian sống của refresh token, tính bằng ngày.</summary>
    public int SoNgaySongRefreshToken { get; set; } = 7;

    /// <summary>
    /// Độ lệch đồng hồ cho phép khi kiểm hạn token, tính bằng giây.
    /// Mặc định của .NET là 5 phút — quá rộng, khiến token đã hết hạn vẫn dùng được.
    /// </summary>
    public int DoLechDongHoGiay { get; set; } = 30;

    /// <summary>
    /// Kiểm cấu hình lúc khởi động. Thiếu key thì dừng ngay, không để lỗi mơ hồ lúc chạy.
    /// </summary>
    public void KiemTra()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                "Thiếu 'Jwt:Key'. Đặt bằng lệnh:\n" +
                "  dotnet user-secrets set \"Jwt:Key\" \"<chuỗi ngẫu nhiên ít nhất 32 ký tự>\" " +
                "--project src/TaskApp.Api");
        }

        // HMAC-SHA256 cần khóa tối thiểu 256 bit. Khóa ngắn hơn sẽ bị thư viện từ chối
        // với thông báo khó hiểu, nên chặn sớm ở đây cho rõ ràng.
        if (Key.Length < 32)
        {
            throw new InvalidOperationException(
                $"'Jwt:Key' quá ngắn ({Key.Length} ký tự). HMAC-SHA256 cần tối thiểu 32 ký tự.");
        }
    }
}
