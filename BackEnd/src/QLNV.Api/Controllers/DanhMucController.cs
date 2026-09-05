using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Mapping;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.1 A4-A6 — danh muc dung chung: tu dien ma-&gt;nhan, cay linh vuc, cay don vi.
///
/// §10.5: he goc KHONG co bang ma-&gt;nhan cho TRANGTHAINV / TRANGTHAIPH trong ma nguon FE;
/// app moi bat buoc seed <c>DM_TUDIEN</c> (§7.4 muc 3) va FE nap qua A4.
/// Moi vai tro dang nhap deu doc duoc danh muc (khong co dong nao cam trong §6.2).
/// </summary>
[Route("api/v1")]
[Authorize]
public sealed class DanhMucController : ApiControllerBase
{
    private readonly DbContext _db;

    public DanhMucController(DbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// A4 — Nạp danh mục theo mã type.
    /// Ví dụ: <c>/api/v1/danh-muc?type=TRANGTHAINV,TRANGTHAIPH</c>.
    /// Bỏ trống tham số type sẽ trả về cả 4 nhóm danh mục.
    /// </summary>
    /// <param name="type">Danh sách mã type, ngăn cách bằng dấu phẩy.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Danh sách mục danh mục, đã sắp xếp theo thứ tự hiển thị.</response>
    /// <response code="400">Có mã type không nằm trong danh sách cho phép.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpGet("danh-muc")]
    [ProducesResponseType(typeof(IReadOnlyList<TuDienDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DanhMucAsync([FromQuery] string? type, CancellationToken ct)
    {
        List<string> dsType;

        if (string.IsNullOrWhiteSpace(type))
        {
            dsType = MaTypeTuDien.ToanBo().ToList();
        }
        else
        {
            dsType = type
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var saiMa = dsType.Where(x => !MaTypeTuDien.HopLe(x)).ToList();
            if (saiMa.Count > 0)
            {
                return LoiDuLieu(
                    $"Mã type không hợp lệ: {string.Join(", ", saiMa)}. " +
                    $"Chỉ chấp nhận: {string.Join(", ", MaTypeTuDien.ToanBo())}.");
            }
        }

        var duLieu = await _db.Set<DmTuDien>()
            .AsNoTracking()
            .Where(x => x.TrangThai == 1 && dsType.Contains(x.Type))
            .OrderBy(x => x.Type)
            .ThenBy(x => x.ThuTu)
            .ThenBy(x => x.Ma)
            .ToListAsync(ct);

        // Dung lambda (khong dung nhom phuong thuc) vi AnhXa.SangDto co nhieu nap chong.
        return Ok(duLieu.Select(x => AnhXa.SangDto(x)).ToList());
    }

    /// <summary>
    /// A5 — Cây lĩnh vực / nghiệp vụ.
    /// Quan hệ cha con lấy từ cột <c>nhomcha</c> — chính là đầu vào của §9.4 S1
    /// (nhánh chiết khấu 50% khi ứng viên chưa từng làm đúng lĩnh vực đó).
    /// </summary>
    /// <param name="baoGomNgungDung">True để lấy cả lĩnh vực đã ngừng dùng (trangthai = 0).</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Cây lĩnh vực, mỗi nút kèm danh sách nút con.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpGet("linh-vuc")]
    [ProducesResponseType(typeof(IReadOnlyList<LinhVucDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LinhVucAsync(
        [FromQuery] bool baoGomNgungDung,
        CancellationToken ct)
    {
        var truyVan = _db.Set<DmLinhVuc>().AsNoTracking();
        if (!baoGomNgungDung)
        {
            truyVan = truyVan.Where(x => x.TrangThai == 1);
        }

        var phang = await truyVan
            .OrderBy(x => x.ThuTu)
            .ThenBy(x => x.Ten)
            .ToListAsync(ct);

        return Ok(AnhXa.DungCayLinhVuc(phang));
    }

    /// <summary>
    /// A6 — Cây đơn vị kèm người dùng của từng đơn vị.
    /// Dùng cho ô chọn người thực hiện ở màn phân công (M05) và cho phạm vi §9.3
    /// (lọc cứng theo <c>phamViUnitCode</c>).
    /// </summary>
    /// <param name="baoGomNgungDung">True để lấy cả đơn vị / tài khoản đã ngừng hoạt động.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Cây đơn vị, mỗi nút kèm danh sách người dùng và đơn vị con.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    [HttpGet("don-vi/cay")]
    [ProducesResponseType(typeof(IReadOnlyList<DonViDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CayDonViAsync(
        [FromQuery] bool baoGomNgungDung,
        CancellationToken ct)
    {
        var truyVanDonVi = _db.Set<SysUnit>().AsNoTracking();
        var truyVanNguoiDung = _db.Set<SysUser>().AsNoTracking();

        if (!baoGomNgungDung)
        {
            truyVanDonVi = truyVanDonVi.Where(x => x.TrangThai == 1);
            truyVanNguoiDung = truyVanNguoiDung.Where(x => x.TrangThai == 1);
        }

        var donVi = await truyVanDonVi
            .OrderBy(x => x.CapDonVi)
            .ThenBy(x => x.TenDonVi)
            .ToListAsync(ct);

        var nguoiDung = await truyVanNguoiDung
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);

        return Ok(AnhXa.DungCayDonVi(donVi, nguoiDung));
    }
}
