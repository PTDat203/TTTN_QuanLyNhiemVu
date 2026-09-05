using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Mapping;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.1 A1-A3 — xac thuc. Thay the SSO WSO2/Keycloak cua he goc bang JWT tu phat hanh
/// (§7.2), secret doc tu cau hinh / bien moi truong (§10.11).
/// </summary>
[Route("api/v1/auth")]
[Authorize]
public sealed class AuthController : ApiControllerBase
{
    private readonly DbContext _db;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _bam;
    private readonly IHienTai _hienTai;
    private readonly ILogger<AuthController> _nhatKy;

    public AuthController(
        DbContext db,
        IJwtService jwt,
        IPasswordHasher bam,
        IHienTai hienTai,
        ILogger<AuthController> nhatKy)
    {
        _db = db;
        _jwt = jwt;
        _bam = bam;
        _hienTai = hienTai;
        _nhatKy = nhatKy;
    }

    /// <summary>
    /// A1 — Đăng nhập bằng tên đăng nhập / mật khẩu.
    /// Trả về access token, refresh token và thông tin người dùng.
    /// </summary>
    /// <param name="yeuCau">Tên đăng nhập và mật khẩu.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Đăng nhập thành công.</response>
    /// <response code="400">Thiếu tên đăng nhập hoặc mật khẩu.</response>
    /// <response code="401">Sai tên đăng nhập hoặc mật khẩu.</response>
    /// <response code="403">Tài khoản đã bị khoá.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DangNhapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DangNhapAsync([FromBody] DangNhapRequest yeuCau, CancellationToken ct)
    {
        if (yeuCau is null || string.IsNullOrWhiteSpace(yeuCau.UserName) || string.IsNullOrWhiteSpace(yeuCau.Password))
        {
            return LoiDuLieu("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
        }

        var tenChuan = yeuCau.UserName.Trim().ToLowerInvariant();

        // KHONG dung Include(): tang API khong duoc phu thuoc cach tang Infrastructure
        // cau hinh navigation SysUser.DonVi. Ten don vi tra cuu bang truy van rieng.
        var nguoiDung = await _db.Set<SysUser>()
            .FirstOrDefaultAsync(x => x.UserName.ToLower() == tenChuan, ct);

        // §10.11: khong phan biet "sai ten" va "sai mat khau" de tranh do ten tai khoan.
        if (nguoiDung is null || !_bam.KiemTra(yeuCau.Password, nguoiDung.PasswordHash))
        {
            _nhatKy.LogWarning("Dang nhap that bai cho ten dang nhap {Ten}", tenChuan);
            return Loi(StatusCodes.Status401Unauthorized,
                "Tên đăng nhập hoặc mật khẩu không đúng.", MaLoiChung.ChuaDangNhap);
        }

        // §9.3 dieu 1 / §4.7: trangthai = 0 la tai khoan bi khoa.
        if (!nguoiDung.DangHoatDong)
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Tài khoản đã bị khoá. Vui lòng liên hệ quản trị hệ thống.", MaLoiChung.KhongCoQuyen);
        }

        var cap = _jwt.PhatHanh(nguoiDung);

        // Luu refresh token vao SYS_USER (xem ghi chu chon cach o JwtService).
        nguoiDung.RefreshToken = cap.RefreshToken;
        nguoiDung.RefreshTokenHetHan = cap.RefreshHetHanLuc;
        await _db.SaveChangesAsync(ct);

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);

        return Ok(new DangNhapResponse
        {
            AccessToken = cap.AccessToken,
            RefreshToken = cap.RefreshToken,
            LoaiToken = cap.LoaiToken,
            HetHanLuc = cap.HetHanLuc,
            NguoiDung = AnhXa.SangDto(nguoiDung, tenDonVi)
        });
    }

    /// <summary>
    /// A2 — Làm mới cặp token bằng refresh token.
    /// Refresh token cũ bị vô hiệu ngay khi cấp cặp mới (xoay vòng).
    /// </summary>
    /// <param name="yeuCau">Refresh token đang hiệu lực. Trường accessToken không bắt buộc.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Cấp cặp token mới thành công.</response>
    /// <response code="400">Thiếu refresh token.</response>
    /// <response code="401">Refresh token không hợp lệ hoặc đã hết hạn.</response>
    /// <response code="403">Tài khoản đã bị khoá.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DangNhapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LamMoiTokenAsync([FromBody] LamMoiTokenRequest yeuCau, CancellationToken ct)
    {
        if (yeuCau is null || string.IsNullOrWhiteSpace(yeuCau.RefreshToken))
        {
            return LoiDuLieu("Thiếu refresh token.");
        }

        var chuoi = yeuCau.RefreshToken.Trim();

        var nguoiDung = await _db.Set<SysUser>()
            .FirstOrDefaultAsync(x => x.RefreshToken == chuoi, ct);

        if (nguoiDung is null
            || nguoiDung.RefreshTokenHetHan is null
            || nguoiDung.RefreshTokenHetHan.Value <= DateTime.UtcNow)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Refresh token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.",
                MaLoiChung.ChuaDangNhap);
        }

        if (!nguoiDung.DangHoatDong)
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Tài khoản đã bị khoá. Vui lòng liên hệ quản trị hệ thống.", MaLoiChung.KhongCoQuyen);
        }

        var cap = _jwt.PhatHanh(nguoiDung);
        nguoiDung.RefreshToken = cap.RefreshToken;
        nguoiDung.RefreshTokenHetHan = cap.RefreshHetHanLuc;
        await _db.SaveChangesAsync(ct);

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);

        return Ok(new DangNhapResponse
        {
            AccessToken = cap.AccessToken,
            RefreshToken = cap.RefreshToken,
            LoaiToken = cap.LoaiToken,
            HetHanLuc = cap.HetHanLuc,
            NguoiDung = AnhXa.SangDto(nguoiDung, tenDonVi)
        });
    }

    /// <summary>
    /// A3 — Đăng xuất: xoá refresh token đang lưu của tài khoản hiện tại.
    /// </summary>
    /// <param name="yeuCau">Không bắt buộc; giữ để tương thích hợp đồng §5.1 A3.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="204">Đăng xuất thành công.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DangXuatAsync([FromBody] DangXuatRequest? yeuCau, CancellationToken ct)
    {
        _ = yeuCau; // Hop dong §5.1 A3 cho phep gui kem refreshToken; ban nay khong can dung.

        var userId = _hienTai.UserId;
        if (userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập.", MaLoiChung.ChuaDangNhap);
        }

        var nguoiDung = await _db.Set<SysUser>().FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
        if (nguoiDung is not null)
        {
            nguoiDung.RefreshToken = null;
            nguoiDung.RefreshTokenHetHan = null;
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    /// <summary>
    /// Thông tin tài khoản đang đăng nhập — phục vụ khôi phục phiên ở phía Angular.
    /// </summary>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Trả về hồ sơ tài khoản hiện tại.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpGet("toi")]
    [ProducesResponseType(typeof(NguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToiAsync(CancellationToken ct)
    {
        var userId = _hienTai.UserId;
        if (userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized, "Bạn chưa đăng nhập.", MaLoiChung.ChuaDangNhap);
        }

        var nguoiDung = await _db.Set<SysUser>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId.Value, ct);

        if (nguoiDung is null)
        {
            return LoiKhongTimThay("Không tìm thấy tài khoản.");
        }

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);
        return Ok(AnhXa.SangDto(nguoiDung, tenDonVi));
    }

    /// <summary>
    /// Đổi mật khẩu của chính tài khoản đang đăng nhập.
    /// Đổi thành công sẽ xoá refresh token cũ, buộc đăng nhập lại trên thiết bị khác.
    /// </summary>
    /// <param name="yeuCau">Mật khẩu cũ và mật khẩu mới.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="204">Đổi mật khẩu thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc mật khẩu cũ không đúng.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpPost("doi-mat-khau")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DoiMatKhauAsync([FromBody] DoiMatKhauRequest yeuCau, CancellationToken ct)
    {
        var userId = _hienTai.UserId;
        if (userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized, "Bạn chưa đăng nhập.", MaLoiChung.ChuaDangNhap);
        }

        if (yeuCau is null || string.IsNullOrWhiteSpace(yeuCau.MatKhauMoi) || yeuCau.MatKhauMoi.Length < 8)
        {
            return LoiDuLieu("Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        var nguoiDung = await _db.Set<SysUser>().FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
        if (nguoiDung is null)
        {
            return LoiKhongTimThay("Không tìm thấy tài khoản.");
        }

        if (!_bam.KiemTra(yeuCau.MatKhauCu ?? string.Empty, nguoiDung.PasswordHash))
        {
            return LoiDuLieu("Mật khẩu hiện tại không đúng.");
        }

        nguoiDung.PasswordHash = _bam.Bam(yeuCau.MatKhauMoi);
        nguoiDung.RefreshToken = null;
        nguoiDung.RefreshTokenHetHan = null;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Tra ten don vi theo ma. Dung truy van rieng thay cho Include() de tang API
    /// khong phu thuoc cau hinh navigation cua tang Infrastructure.
    /// </summary>
    private Task<string?> TenDonViAsync(string unitCode, CancellationToken ct) =>
        _db.Set<SysUnit>()
            .AsNoTracking()
            .Where(x => x.UnitCode == unitCode)
            .Select(x => x.TenDonVi)
            .FirstOrDefaultAsync(ct);
}
