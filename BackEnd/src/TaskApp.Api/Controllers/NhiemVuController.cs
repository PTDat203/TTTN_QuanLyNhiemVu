using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;

namespace TaskApp.Api.Controllers;

/// <summary>
/// Quản lý nhiệm vụ: tra cứu, tạo, sửa, xóa, giao việc, tiếp nhận.
///
/// <para>
/// Danh tính người gọi luôn lấy từ token, không bao giờ lấy từ thân yêu cầu — nếu tin
/// <c>userId</c> do client gửi lên thì ai cũng mạo danh được người khác.
/// </para>
/// </summary>
[ApiController]
[Route("api/nhiem-vu")]
[Authorize]
[Produces("application/json")]
public sealed class NhiemVuController : ControllerBase
{
    private readonly NhiemVuService _service;
    private readonly NguoiDungHienTai _hienTai;

    public NhiemVuController(NhiemVuService service, NguoiDungHienTai hienTai)
    {
        _service = service;
        _hienTai = hienTai;
    }

    /// <summary>Danh sách nhiệm vụ có lọc, sắp xếp và phân trang.</summary>
    /// <remarks>
    /// MANAGER thấy nhiệm vụ mình tạo, EMPLOYEE thấy nhiệm vụ được giao cho mình.
    /// Giới hạn này áp ngay trong truy vấn SQL.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(KetQuaPhanTrang<NhiemVuTomTatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DanhSach([FromQuery] NhiemVuLocRequest loc, CancellationToken ct)
    {
        var kq = await _service.DanhSachAsync(
            loc, _hienTai.LayUserIdBatBuoc(), _hienTai.VaiTroHienTai ?? string.Empty, ct);
        return Ok(kq);
    }

    /// <summary>Chi tiết một nhiệm vụ kèm lịch sử tiến độ, báo cáo và tệp đính kèm.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(NhiemVuChiTietDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChiTiet(long id, CancellationToken ct)
    {
        var kq = await _service.ChiTietAsync(
            id, _hienTai.LayUserIdBatBuoc(), _hienTai.VaiTroHienTai ?? string.Empty, ct);
        return TraVe(kq);
    }

    /// <summary>Tạo nhiệm vụ mới. Chỉ MANAGER.</summary>
    /// <remarks>
    /// Trạng thái khởi tạo do server đặt: có <c>assigneeId</c> thì <c>DA_GIAO</c>,
    /// không thì <c>MOI_TAO</c>. Client không gửi trạng thái lên được.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(typeof(NhiemVuChiTietDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Tao([FromBody] TaoNhiemVuRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.TaoAsync(yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        if (!kq.ThanhCong) return TraVe(kq);

        return CreatedAtAction(nameof(ChiTiet), new { id = kq.DuLieu!.Id }, kq.DuLieu);
    }

    /// <summary>Sửa nhiệm vụ. Chỉ người tạo, và chỉ khi còn ở "Mới tạo" hoặc "Đã giao".</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(typeof(NhiemVuChiTietDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sua(long id, [FromBody] SuaNhiemVuRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.SuaAsync(id, yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Xóa nhiệm vụ. Chỉ người tạo, chỉ khi còn "Mới tạo" và chưa có dữ liệu con.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Xoa(long id, CancellationToken ct)
    {
        var kq = await _service.XoaAsync(id, _hienTai.LayUserIdBatBuoc(), ct);
        return kq.ThanhCong ? NoContent() : TraVe(kq);
    }

    /// <summary>Giao nhiệm vụ cho một người thực hiện. Chỉ MANAGER.</summary>
    /// <remarks>Người nhận phải là EMPLOYEE đang hoạt động.</remarks>
    [HttpPost("{id:long}/giao")]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(typeof(NhiemVuChiTietDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Giao(long id, [FromBody] GiaoNhiemVuRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.GiaoAsync(id, yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Tiếp nhận nhiệm vụ được giao. Chỉ chính người được giao.</summary>
    [HttpPost("{id:long}/tiep-nhan")]
    [ProducesResponseType(typeof(NhiemVuChiTietDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TiepNhan(long id, CancellationToken ct)
    {
        var kq = await _service.TiepNhanAsync(id, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    // ------------------------------------------------------------------
    private IActionResult TraVe<T>(KetQua<T> kq)
        => kq.ThanhCong ? Ok(kq.DuLieu) : LoiHttp(kq.MaLoi, kq.Loi);

    private IActionResult TraVe(KetQua kq)
        => kq.ThanhCong ? NoContent() : LoiHttp(kq.MaLoi, kq.Loi);

    private IActionResult LoiHttp(string? maLoi, string? thongBao)
    {
        var maHttp = maLoi switch
        {
            MaLoiChung.KhongCoQuyen => StatusCodes.Status403Forbidden,
            MaLoiChung.KhongTimThay => StatusCodes.Status404NotFound,
            MaLoiChung.ChuaXacThuc => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(detail: thongBao, statusCode: maHttp, title: "Yêu cầu không thực hiện được");
    }
}
