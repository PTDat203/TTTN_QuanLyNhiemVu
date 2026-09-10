using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;

namespace TaskApp.Api.Controllers;

/// <summary>Xác thực: đăng nhập, làm mới token, đăng xuất, đổi mật khẩu.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly NguoiDungHienTai _hienTai;

    public AuthController(AuthService auth, NguoiDungHienTai hienTai)
    {
        _auth = auth;
        _hienTai = hienTai;
    }

    /// <summary>Đăng nhập bằng tên đăng nhập và mật khẩu.</summary>
    /// <remarks>
    /// Tài khoản demo, mật khẩu chung <c>123456</c>:
    /// <c>manager1</c>, <c>manager2</c> (MANAGER) — <c>nv.an</c>, <c>nv.binh</c>, … (EMPLOYEE).
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DangNhapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DangNhap(
        [FromBody] DangNhapRequest yeuCau, CancellationToken ct)
    {
        var kq = await _auth.DangNhapAsync(yeuCau, ct);
        return TraVe(kq);
    }

    /// <summary>Đổi refresh token lấy cặp token mới.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DangNhapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LamMoi(
        [FromBody] LamMoiTokenRequest yeuCau, CancellationToken ct)
    {
        var kq = await _auth.LamMoiAsync(yeuCau, ct);
        return TraVe(kq);
    }

    /// <summary>Đăng xuất — thu hồi refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DangXuat([FromBody] DangXuatRequest yeuCau)
    {
        _auth.DangXuat(yeuCau);
        return NoContent();
    }

    /// <summary>Hồ sơ của người đang đăng nhập.</summary>
    [HttpGet("toi")]
    [Authorize]
    [ProducesResponseType(typeof(NguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HoSoCuaToi(CancellationToken ct)
    {
        var kq = await _auth.LayHoSoAsync(_hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Đổi mật khẩu của chính mình. Đổi xong phải đăng nhập lại.</summary>
    [HttpPost("doi-mat-khau")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DoiMatKhau(
        [FromBody] DoiMatKhauRequest yeuCau, CancellationToken ct)
    {
        var kq = await _auth.DoiMatKhauAsync(_hienTai.LayUserIdBatBuoc(), yeuCau, ct);
        return kq.ThanhCong ? NoContent() : TraVe(kq);
    }

    // ------------------------------------------------------------------
    // Ánh xạ KetQua sang mã HTTP
    // ------------------------------------------------------------------
    private IActionResult TraVe<T>(KetQua<T> kq)
        => kq.ThanhCong ? Ok(kq.DuLieu) : LoiHttp(kq.MaLoi, kq.Loi);

    private IActionResult TraVe(KetQua kq)
        => kq.ThanhCong ? NoContent() : LoiHttp(kq.MaLoi, kq.Loi);

    private IActionResult LoiHttp(string? maLoi, string? thongBao)
    {
        var maHttp = maLoi switch
        {
            MaLoiChung.XacThucThatBai => StatusCodes.Status401Unauthorized,
            MaLoiChung.ChuaXacThuc => StatusCodes.Status401Unauthorized,
            MaLoiChung.KhongCoQuyen => StatusCodes.Status403Forbidden,
            MaLoiChung.KhongTimThay => StatusCodes.Status404NotFound,
            MaLoiChung.DuLieuKhongHopLe => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(detail: thongBao, statusCode: maHttp, title: "Yêu cầu không thực hiện được");
    }
}
