using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Services;

/// <summary>
/// Nghiệp vụ xác thực: đăng nhập, làm mới token, đăng xuất, đổi mật khẩu.
/// </summary>
public sealed class AuthService
{
    private readonly TaskDbContext _db;
    private readonly JwtService _jwt;
    private readonly ILogger<AuthService> _log;

    public AuthService(TaskDbContext db, JwtService jwt, ILogger<AuthService> log)
    {
        _db = db;
        _jwt = jwt;
        _log = log;
    }

    /// <summary>
    /// Đăng nhập bằng username và mật khẩu.
    ///
    /// <para>
    /// Thông báo lỗi cố tình dùng chung một câu cho cả "sai tên" lẫn "sai mật khẩu".
    /// Tách hai câu ra sẽ để lộ tài khoản nào có thật, giúp kẻ tấn công dò danh sách
    /// người dùng trước khi thử mật khẩu.
    /// </para>
    /// </summary>
    public async Task<KetQua<DangNhapResponse>> DangNhapAsync(
        DangNhapRequest yeuCau, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.Username) || string.IsNullOrWhiteSpace(yeuCau.Password))
        {
            return KetQua<DangNhapResponse>.DuLieuKhongHopLe(
                "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
        }

        var username = yeuCau.Username.Trim();

        var nguoiDung = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        // So khớp mật khẩu ngay cả khi không tìm thấy người dùng, để thời gian phản hồi
        // của hai nhánh gần bằng nhau. Nếu thoát sớm, kẻ tấn công đo thời gian là biết
        // tài khoản nào tồn tại.
        var hashDeSoSanh = nguoiDung?.PasswordHash ?? MatKhauHasher.HashGia;
        var dungMatKhau = MatKhauHasher.KiemTra(yeuCau.Password, hashDeSoSanh);

        if (nguoiDung is null || !dungMatKhau)
        {
            _log.LogWarning("Đăng nhập thất bại cho tên: {Username}", username);
            return KetQua<DangNhapResponse>.ThatBai(
                "Tên đăng nhập hoặc mật khẩu không đúng.", MaLoiChung.XacThucThatBai);
        }

        if (nguoiDung.Status != TrangThaiNguoiDung.HoatDong)
        {
            return KetQua<DangNhapResponse>.ThatBai(
                "Tài khoản đã bị ngừng hoạt động. Liên hệ quản trị viên để được mở lại.",
                MaLoiChung.XacThucThatBai);
        }

        _log.LogInformation("Đăng nhập thành công: {Username} ({VaiTro})",
            nguoiDung.Username, nguoiDung.Role);

        return KetQua<DangNhapResponse>.Ok(TaoPhanHoi(nguoiDung));
    }

    /// <summary>
    /// Làm mới cặp token. Refresh token cũ bị thu hồi ngay khi dùng (xoay vòng token).
    /// </summary>
    public async Task<KetQua<DangNhapResponse>> LamMoiAsync(
        LamMoiTokenRequest yeuCau, CancellationToken ct = default)
    {
        var userId = _jwt.DoiRefreshToken(yeuCau.RefreshToken);
        if (userId is null)
        {
            return KetQua<DangNhapResponse>.ThatBai(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", MaLoiChung.ChuaXacThuc);
        }

        var nguoiDung = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

        // Tài khoản có thể đã bị xóa hoặc khóa sau khi token được cấp — phải kiểm lại,
        // không tin token là đủ.
        if (nguoiDung is null || nguoiDung.Status != TrangThaiNguoiDung.HoatDong)
        {
            return KetQua<DangNhapResponse>.ThatBai(
                "Tài khoản không còn hiệu lực. Vui lòng đăng nhập lại.", MaLoiChung.ChuaXacThuc);
        }

        return KetQua<DangNhapResponse>.Ok(TaoPhanHoi(nguoiDung));
    }

    /// <summary>Đăng xuất — thu hồi refresh token đang giữ.</summary>
    public KetQua DangXuat(DangXuatRequest yeuCau)
    {
        _jwt.ThuHoi(yeuCau.RefreshToken);
        // Luôn trả thành công: token không tồn tại thì mục tiêu "phiên này không dùng được nữa"
        // vẫn đạt. Báo lỗi chỉ tổ để lộ token nào còn hiệu lực.
        return KetQua.Ok();
    }

    /// <summary>
    /// Đổi mật khẩu của chính người đang đăng nhập.
    /// Đổi xong thu hồi toàn bộ refresh token của người đó, buộc đăng nhập lại trên mọi thiết bị.
    /// </summary>
    public async Task<KetQua> DoiMatKhauAsync(
        long userId, DoiMatKhauRequest yeuCau, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.MatKhauMoi) || yeuCau.MatKhauMoi.Length < 6)
        {
            return KetQua.DuLieuKhongHopLe("Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        if (yeuCau.MatKhauCu == yeuCau.MatKhauMoi)
        {
            return KetQua.DuLieuKhongHopLe("Mật khẩu mới phải khác mật khẩu cũ.");
        }

        var nguoiDung = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (nguoiDung is null)
        {
            return KetQua.KhongTimThay("Không tìm thấy tài khoản.");
        }

        if (!MatKhauHasher.KiemTra(yeuCau.MatKhauCu, nguoiDung.PasswordHash))
        {
            return KetQua.ThatBai("Mật khẩu cũ không đúng.", MaLoiChung.XacThucThatBai);
        }

        nguoiDung.PasswordHash = MatKhauHasher.Hash(yeuCau.MatKhauMoi);
        await _db.SaveChangesAsync(ct);

        var soTokenThuHoi = _jwt.ThuHoiTatCa(userId);
        _log.LogInformation("Người dùng {UserId} đổi mật khẩu, thu hồi {So} refresh token",
            userId, soTokenThuHoi);

        return KetQua.Ok();
    }

    /// <summary>Lấy hồ sơ của người đang đăng nhập.</summary>
    public async Task<KetQua<NguoiDungDto>> LayHoSoAsync(long userId, CancellationToken ct = default)
    {
        var nguoiDung = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return nguoiDung is null
            ? KetQua<NguoiDungDto>.KhongTimThay("Không tìm thấy tài khoản.")
            : KetQua<NguoiDungDto>.Ok(ChuyenDoi(nguoiDung));
    }

    private DangNhapResponse TaoPhanHoi(User nguoiDung)
    {
        var cap = _jwt.PhatHanh(nguoiDung);
        return new DangNhapResponse
        {
            AccessToken = cap.AccessToken,
            RefreshToken = cap.RefreshToken,
            HetHanLuc = cap.HetHanLuc,
            NguoiDung = ChuyenDoi(nguoiDung)
        };
    }

    /// <summary>Chuyển thực thể sang DTO. Không bao giờ mang PASSWORD_HASH ra ngoài.</summary>
    public static NguoiDungDto ChuyenDoi(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        TenVaiTro = VaiTro.TenHienThi(u.Role),
        Status = u.Status
    };
}
