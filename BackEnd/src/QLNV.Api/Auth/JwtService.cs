using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Auth;

/// <summary>
/// §5.1 A1-A3 / §7.2 — cai dat <see cref="IJwtService"/>: JWT tu phat hanh, HS256.
///
/// CHON CACH LUU REFRESH TOKEN (ghi ro theo yeu cau):
/// refresh token luu THANG vao cot <c>SYS_USER.refresh_token</c> +
/// <c>SYS_USER.refresh_token_hethan</c> (§4.7, hai truong da co san trong
/// <see cref="SysUser"/>). Moi tai khoan chi giu MOT refresh token dang hieu luc,
/// dang nhap moi hoac lam moi token se xoay vong (rotate) gia tri cu.
/// Uu diem: khong can bang phu, dang xuat la xoa mot cot; nhuoc diem: khong ho tro
/// nhieu thiet bi song song — chap nhan duoc voi pham vi app thuc tap.
/// Viec ghi/xoa cot do <c>AuthController</c> dam nhiem (tang nay khong cham CSDL).
/// </summary>
public sealed class JwtService : IJwtService
{
    private readonly CauHinhJwt _cauHinh;
    private readonly JwtSecurityTokenHandler _boXuLy = new();

    public JwtService(CauHinhJwt cauHinh)
    {
        _cauHinh = cauHinh ?? throw new ArgumentNullException(nameof(cauHinh));
    }

    /// <inheritdoc />
    public int SoPhutSongAccessToken => _cauHinh.SoPhutSongAccessToken;

    /// <inheritdoc />
    public int SoNgaySongRefreshToken => _cauHinh.SoNgaySongRefreshToken;

    /// <inheritdoc />
    public CapTokenDto PhatHanh(SysUser nguoiDung)
    {
        ArgumentNullException.ThrowIfNull(nguoiDung);

        var bayGio = DateTime.UtcNow;
        var hetHan = bayGio.AddMinutes(_cauHinh.SoPhutSongAccessToken);

        var dsClaim = new List<Claim>
        {
            new(TenClaim.UserId, nguoiDung.Id.ToString()),
            new(TenClaim.Jti, Guid.NewGuid().ToString("N")),
            new(TenClaim.UserName, nguoiDung.UserName),
            new(TenClaim.FullName, nguoiDung.FullName),
            // §6.1: MOT truc vai tro duy nhat
            new(TenClaim.VaiTro, nguoiDung.VaiTro),
            new(TenClaim.UnitCode, nguoiDung.UnitCode)
        };

        var khoa = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_cauHinh.Key));
        var token = new JwtSecurityToken(
            issuer: _cauHinh.Issuer,
            audience: _cauHinh.Audience,
            claims: dsClaim,
            notBefore: bayGio,
            expires: hetHan,
            signingCredentials: new SigningCredentials(khoa, SecurityAlgorithms.HmacSha256));

        return new CapTokenDto
        {
            AccessToken = _boXuLy.WriteToken(token),
            RefreshToken = TaoRefreshToken(),
            LoaiToken = "Bearer",
            HetHanLuc = hetHan,
            RefreshHetHanLuc = bayGio.AddDays(_cauHinh.SoNgaySongRefreshToken)
        };
    }

    /// <inheritdoc />
    public string TaoRefreshToken()
    {
        // 48 byte ngau nhien an toan mat ma -> 64 ky tu Base64.
        var byteNgauNhien = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(byteNgauNhien);
    }

    /// <inheritdoc />
    public Result<Guid> DocUserId(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result<Guid>.ThatBai("Thiếu access token.", MaLoiChung.ChuaDangNhap);
        }

        try
        {
            var thamSo = ThamSoKiemTraToken.Tao(_cauHinh, kiemTraHan: true);
            var chuThe = _boXuLy.ValidateToken(accessToken, thamSo, out _);

            var chuoiId = chuThe.FindFirst(TenClaim.UserId)?.Value
                          ?? chuThe.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(chuoiId, out var userId))
            {
                return Result<Guid>.ThatBai(
                    "Token không chứa định danh người dùng hợp lệ.", MaLoiChung.ChuaDangNhap);
            }

            return Result<Guid>.Ok(userId);
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<Guid>.ThatBai(
                "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.", MaLoiChung.ChuaDangNhap);
        }
        catch (SecurityTokenException)
        {
            return Result<Guid>.ThatBai(
                "Token không hợp lệ hoặc đã bị thay đổi.", MaLoiChung.ChuaDangNhap);
        }
        catch (ArgumentException)
        {
            // Chuoi token sai dinh dang (khong phai JWT ba phan).
            return Result<Guid>.ThatBai("Token sai định dạng.", MaLoiChung.ChuaDangNhap);
        }
    }
}
