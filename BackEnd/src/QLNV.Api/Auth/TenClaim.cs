namespace QLNV.Api.Auth;

/// <summary>
/// Ten cac claim trong access token.
///
/// LUU Y: Program.cs goi <c>JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()</c>
/// nen ten claim KHONG bi anh xa sang URI dai cua WS-Federation. Doc thang bang ten o day.
/// §6.1: chi MOT truc vai tro (<see cref="VaiTro"/>), khong bo 3 truc chong cheo cua he goc.
/// </summary>
public static class TenClaim
{
    /// <summary>Dinh danh nguoi dung (Guid dang chuoi).</summary>
    public const string UserId = "sub";

    /// <summary>Dinh danh duy nhat cua token.</summary>
    public const string Jti = "jti";

    /// <summary>Ten dang nhap.</summary>
    public const string UserName = "username";

    /// <summary>Ho ten day du — chi de hien thi.</summary>
    public const string FullName = "fullname";

    /// <summary>QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN (§6.1).</summary>
    public const string VaiTro = "vaitro";

    /// <summary>Ma don vi — pham vi du lieu (§6.2 dong 20, 22).</summary>
    public const string UnitCode = "unitcode";
}
