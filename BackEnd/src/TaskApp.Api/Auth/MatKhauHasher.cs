namespace TaskApp.Api.Auth;

/// <summary>
/// Băm và kiểm tra mật khẩu bằng BCrypt.
///
/// Không tự cài thuật toán băm. Mật khẩu trong <c>USERS.PASSWORD_HASH</c> là chuỗi BCrypt
/// dạng <c>$2a$11$...</c>, đã tự mang muối và hệ số công việc bên trong nên không cần
/// cột muối riêng.
/// </summary>
public static class MatKhauHasher
{
    /// <summary>
    /// Hệ số công việc. 11 nghĩa là 2^11 vòng lặp — đủ chậm để chống dò, đủ nhanh để
    /// đăng nhập không thấy trễ. Phải khớp với hash trong script seed.
    /// </summary>
    private const int HeSoCongViec = 11;

    /// <summary>
    /// Hash của một mật khẩu không ai dùng, để so khớp giả khi không tìm thấy tài khoản.
    ///
    /// Mục đích: giữ thời gian phản hồi của hai nhánh "không có tài khoản" và
    /// "sai mật khẩu" gần bằng nhau. Nếu thoát sớm khi không tìm thấy người dùng thì
    /// nhánh đó nhanh hơn hẳn, và đo thời gian phản hồi là biết tài khoản nào tồn tại.
    /// </summary>
    public const string HashGia = "$2a$11$AKBHjhv/BsthyD9NZvO2.ug9i/USnJmxcI7M/Ym/mIBV9YUz1Anhi";

    /// <summary>Băm mật khẩu rõ thành chuỗi BCrypt.</summary>
    public static string Hash(string matKhau)
        => BCrypt.Net.BCrypt.HashPassword(matKhau, HeSoCongViec);

    /// <summary>
    /// Kiểm tra mật khẩu rõ với hash đã lưu.
    /// Hash hỏng hoặc sai định dạng thì trả false chứ không ném lỗi — dữ liệu xấu trong DB
    /// không được phép làm sập cả endpoint đăng nhập.
    /// </summary>
    public static bool KiemTra(string matKhau, string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(matKhau, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
