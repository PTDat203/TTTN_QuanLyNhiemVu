using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;

namespace TaskApp.Api.Controllers;

/// <summary>Tra cứu người dùng. Chỉ đọc — việc tạo tài khoản nằm ngoài phạm vi giai đoạn này.</summary>
[ApiController]
[Route("api/nguoi-dung")]
[Authorize]
[Produces("application/json")]
public sealed class NguoiDungController : ControllerBase
{
    private readonly TaskDbContext _db;

    public NguoiDungController(TaskDbContext db) => _db = db;

    /// <summary>
    /// Danh sách nhân viên đang hoạt động, dùng cho ô chọn người thực hiện khi giao việc.
    /// </summary>
    /// <remarks>
    /// Chỉ MANAGER gọi được. Chỉ trả những người thực sự nhận việc được
    /// (<c>USER_ROLE = EMPLOYEE</c> và <c>USER_STATUS = ACTIVE</c>), để giao diện không
    /// bày ra lựa chọn mà backend sẽ từ chối.
    /// </remarks>
    [HttpGet("nhan-vien")]
    [Authorize(Roles = VaiTro.NhomGiaoViec)]
    [ProducesResponseType(typeof(IReadOnlyList<NguoiDungDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DanhSachNhanVien(CancellationToken ct)
    {
        var ds = await _db.Users.AsNoTracking()
            .Where(u => (u.Role == VaiTro.TruongNhom || u.Role == VaiTro.NhanVien)
                        && u.Status == TrangThaiNguoiDung.HoatDong)
            .OrderBy(u => u.FullName)
            .Select(u => new NguoiDungDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                TenVaiTro = VaiTro.TenHienThi(u.Role),
                Status = u.Status
            })
            .ToListAsync(ct);

        return Ok(ds);
    }

    /// <summary>Kỹ năng đã khai của một người — dữ liệu đầu vào của mô hình gợi ý.</summary>
    [HttpGet("{id:long}/ky-nang")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> KyNang(long id, CancellationToken ct)
    {
        var ds = await _db.UserSkills.AsNoTracking()
            .Where(s => s.UserId == id)
            .OrderByDescending(s => s.SkillLevel)
            .Select(s => new { s.Id, s.SkillName, s.SkillLevel, s.Description })
            .ToListAsync(ct);

        return Ok(ds);
    }
}
