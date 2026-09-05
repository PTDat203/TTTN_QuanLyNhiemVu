using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace QLNV.Api.Auth;

/// <summary>
/// Noi DUY NHAT dung tham so kiem tra token, de Program.cs (middleware JwtBearer)
/// va <see cref="JwtService"/> khong bao gio lech nhau.
/// </summary>
public static class ThamSoKiemTraToken
{
    /// <summary>
    /// Dung <see cref="TokenValidationParameters"/> tu cau hinh.
    /// </summary>
    /// <param name="cauHinh">Cau hinh JWT da nap va da kiem tra co khoa.</param>
    /// <param name="kiemTraHan">True: tu choi token het han (mac dinh).</param>
    public static TokenValidationParameters Tao(CauHinhJwt cauHinh, bool kiemTraHan = true)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cauHinh.Issuer,
            ValidateAudience = true,
            ValidAudience = cauHinh.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cauHinh.Key)),
            ValidateLifetime = kiemTraHan,
            ClockSkew = TimeSpan.FromSeconds(cauHinh.DoLechDongHoGiay),

            // Giu nguyen ten claim tu tu do phat hanh (§6.1: mot truc vai tro duy nhat).
            NameClaimType = TenClaim.UserName,
            RoleClaimType = TenClaim.VaiTro
        };
    }
}
