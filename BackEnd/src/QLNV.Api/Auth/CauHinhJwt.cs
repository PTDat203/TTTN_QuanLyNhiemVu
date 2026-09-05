namespace QLNV.Api.Auth;

/// <summary>
/// Cau hinh phat hanh JWT, doc tu muc <c>Jwt</c> cua appsettings HOAC tu bien
/// moi truong <c>QLNV_JWT_KEY</c> (uu tien bien moi truong).
///
/// §10.11 — rui ro bao mat cua he goc: secret SSO nam trong <c>environment.ts</c>.
/// App moi TUYET DOI khong dat gia tri mac dinh cho <see cref="Key"/> trong ma nguon;
/// thieu khoa thi nem loi ngay luc khoi dong (xem Program.cs).
/// </summary>
public sealed class CauHinhJwt
{
    /// <summary>Ten muc cau hinh trong appsettings.json.</summary>
    public const string TenMuc = "Jwt";

    /// <summary>
    /// Khoa ky doi xung (HMAC-SHA256). KHONG co gia tri mac dinh — bat buoc cau hinh.
    /// Toi thieu 32 byte (256 bit) theo yeu cau cua HS256.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Ben phat hanh token.</summary>
    public string Issuer { get; set; } = "QLNV.Api";

    /// <summary>Ben tieu thu token (SPA Angular).</summary>
    public string Audience { get; set; } = "QLNV.Web";

    /// <summary>Thoi gian song access token, tinh bang phut. Mac dinh 60.</summary>
    public int SoPhutSongAccessToken { get; set; } = 60;

    /// <summary>Thoi gian song refresh token, tinh bang ngay. Mac dinh 7.</summary>
    public int SoNgaySongRefreshToken { get; set; } = 7;

    /// <summary>Do lech dong ho cho phep khi kiem han token, tinh bang giay.</summary>
    public int DoLechDongHoGiay { get; set; } = 30;
}
