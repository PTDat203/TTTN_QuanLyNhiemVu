using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;

namespace TaskApp.Api.Controllers;

/// <summary>
/// Xử lý nhiệm vụ: cập nhật tiến độ, gửi báo cáo, duyệt báo cáo.
/// Đây là nửa sau của vòng nghiệp vụ, nối tiếp phần giao việc ở <see cref="NhiemVuController"/>.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class XuLyNhiemVuController : ControllerBase
{
    private readonly XuLyNhiemVuService _service;
    private readonly NguoiDungHienTai _hienTai;

    public XuLyNhiemVuController(XuLyNhiemVuService service, NguoiDungHienTai hienTai)
    {
        _service = service;
        _hienTai = hienTai;
    }

    // ---------------------------------------------------------------- tiến độ

    /// <summary>Ghi một lần cập nhật tiến độ. Chỉ người được giao.</summary>
    /// <remarks>
    /// Luôn thêm dòng mới, không ghi đè lịch sử. Nếu nhiệm vụ đang bị "Yêu cầu bổ sung"
    /// thì cập nhật tiến độ sẽ đưa nhiệm vụ trở lại "Đang thực hiện".
    /// </remarks>
    [HttpPost("api/nhiem-vu/{taskId:long}/tien-do")]
    [ProducesResponseType(typeof(TienDoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CapNhatTienDo(
        long taskId, [FromBody] CapNhatTienDoRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.CapNhatTienDoAsync(taskId, yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Lịch sử cập nhật tiến độ của một nhiệm vụ.</summary>
    [HttpGet("api/nhiem-vu/{taskId:long}/tien-do")]
    [ProducesResponseType(typeof(IReadOnlyList<TienDoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LichSuTienDo(long taskId, CancellationToken ct)
    {
        var kq = await _service.LichSuTienDoAsync(
            taskId, _hienTai.LayUserIdBatBuoc(), _hienTai.VaiTroHienTai ?? string.Empty, ct);
        return TraVe(kq);
    }

    // ---------------------------------------------------------------- báo cáo

    /// <summary>Gửi báo cáo kết quả. Chỉ người được giao, khi nhiệm vụ đang "Đang thực hiện".</summary>
    [HttpPost("api/nhiem-vu/{taskId:long}/bao-cao")]
    [ProducesResponseType(typeof(BaoCaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GuiBaoCao(
        long taskId, [FromBody] GuiBaoCaoRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.GuiBaoCaoAsync(taskId, yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Danh sách báo cáo của một nhiệm vụ.</summary>
    [HttpGet("api/nhiem-vu/{taskId:long}/bao-cao")]
    [ProducesResponseType(typeof(IReadOnlyList<BaoCaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DanhSachBaoCao(long taskId, CancellationToken ct)
    {
        var kq = await _service.DanhSachBaoCaoAsync(
            taskId, _hienTai.LayUserIdBatBuoc(), _hienTai.VaiTroHienTai ?? string.Empty, ct);
        return TraVe(kq);
    }

    /// <summary>
    /// Duyệt một báo cáo. Chỉ người giao nhiệm vụ.
    /// Xác nhận thì nhiệm vụ "Hoàn thành"; từ chối thì chuyển "Yêu cầu bổ sung" và bắt buộc nêu lý do.
    /// </summary>
    [HttpPost("api/bao-cao/{baoCaoId:long}/duyet")]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(typeof(BaoCaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DuyetBaoCao(
        long baoCaoId, [FromBody] DuyetBaoCaoRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.DuyetBaoCaoAsync(baoCaoId, yeuCau, _hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    /// <summary>Các báo cáo đang chờ chính tôi duyệt — hộp việc cần xử lý của người giao.</summary>
    [HttpGet("api/bao-cao/cho-toi-duyet")]
    [Authorize(Roles = VaiTro.Manager)]
    [ProducesResponseType(typeof(IReadOnlyList<BaoCaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChoToiDuyet(CancellationToken ct)
    {
        var kq = await _service.ChoToiDuyetAsync(_hienTai.LayUserIdBatBuoc(), ct);
        return TraVe(kq);
    }

    // ----------------------------------------------------------------
    private IActionResult TraVe<T>(KetQua<T> kq)
        => kq.ThanhCong ? Ok(kq.DuLieu) : LoiHttp(kq.MaLoi, kq.Loi);

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
