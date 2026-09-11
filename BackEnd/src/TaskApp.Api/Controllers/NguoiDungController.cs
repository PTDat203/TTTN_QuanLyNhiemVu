using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;

namespace TaskApp.Api.Controllers;

/// <summary>Tra cứu người dùng. Chỉ đọc — việc tạo tài khoản nằm ngoài phạm vi giai đoạn này.</summary>
[ApiController]
[Route("api/nguoi-dung")]
[Authorize]
[Produces("application/json")]
public sealed class NguoiDungController : ControllerBase
{
    private readonly TaskDbContext _db;
    private readonly NguoiDungHienTai _hienTai;

    public NguoiDungController(TaskDbContext db, NguoiDungHienTai hienTai)
    {
        _db = db;
        _hienTai = hienTai;
    }

    /// <summary>
    /// Những người mà người đang đăng nhập được giao việc cho — dùng cho ô chọn người thực
    /// hiện khi giao việc.
    /// </summary>
    /// <remarks>
    /// Dùng đúng quy tắc phạm vi của backend (<see cref="PhamViToChuc"/>): trưởng nhóm chỉ thấy
    /// người trong nhóm, trưởng phòng thấy người trong phòng, Giám đốc thấy mọi cấp dưới.
    /// Giao diện không bao giờ bày ra lựa chọn mà backend sẽ từ chối.
    /// </remarks>
    [HttpGet("nhan-vien")]
    [Authorize(Roles = VaiTro.NhomGiaoViec)]
    [ProducesResponseType(typeof(IReadOnlyList<NguoiDungDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DanhSachNhanVien(CancellationToken ct)
    {
        var viTri = await PhamViToChuc.NapAsync(_db, _hienTai.LayUserIdBatBuoc(), ct);
        if (viTri is null) return Ok(Array.Empty<NguoiDungDto>());

        var ds = await _db.Users.AsNoTracking()
            .NguoiNhanDuoc(viTri)
            .OrderBy(u => u.FullName)
            .Select(u => new NguoiDungDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                TenVaiTro = VaiTro.TenHienThi(u.Role),
                Status = u.Status,
                JobTitle = u.JobTitle,
                DepartmentId = u.DepartmentId,
                TenPhongBan = u.Department != null ? u.Department.Name : null,
                TeamId = u.TeamId,
                TenNhom = u.Team != null ? u.Team.Name : null
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
