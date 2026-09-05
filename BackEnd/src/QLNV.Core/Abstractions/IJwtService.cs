using QLNV.Core.Common;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §5.1 A1-A3 / §7.2 — phat hanh va kiem tra JWT tu ky (access + refresh).
///
/// §10.11 (rui ro bao mat cua he goc — KHONG duoc lap lai): secret PHAI doc tu cau hinh
/// hoac bien moi truong. TUYET DOI khong hard-code trong ma nguon, khong commit vao repo.
/// </summary>
public interface IJwtService
{
    /// <summary>Phat hanh cap access + refresh token cho nguoi dung.</summary>
    CapTokenDto PhatHanh(SysUser nguoiDung);

    /// <summary>Sinh chuoi refresh token ngau nhien an toan mat ma.</summary>
    string TaoRefreshToken();

    /// <summary>
    /// Doc <c>userId</c> tu access token. Tra loi nghiep vu (khong nem) khi token sai
    /// chu ky, sai dinh dang hoac het han.
    /// </summary>
    Result<Guid> DocUserId(string accessToken);

    /// <summary>Thoi gian song cua access token, tinh bang phut.</summary>
    int SoPhutSongAccessToken { get; }

    /// <summary>Thoi gian song cua refresh token, tinh bang ngay.</summary>
    int SoNgaySongRefreshToken { get; }
}
