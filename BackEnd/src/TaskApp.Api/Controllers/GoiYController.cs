using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;

namespace TaskApp.Api.Controllers;

/// <summary>
/// AI gợi ý người thực hiện phù hợp — chức năng trọng tâm của đề tài.
///
/// <para>
/// Hệ thống chỉ <b>đề xuất</b>. Người giao nhiệm vụ vẫn là người quyết định cuối cùng,
/// nên mọi kết quả đều kèm điểm thành phần và lý do để họ tự đánh giá.
/// </para>
/// </summary>
[ApiController]
[Route("api/goi-y")]
[Authorize(Roles = VaiTro.Manager)]
[Produces("application/json")]
public sealed class GoiYController : ControllerBase
{
    private readonly GoiYService _service;
    private readonly NguoiDungHienTai _hienTai;
    private readonly CauHinhGoiY _cauHinh;

    public GoiYController(GoiYService service, NguoiDungHienTai hienTai, IOptions<CauHinhGoiY> cauHinh)
    {
        _service = service;
        _hienTai = hienTai;
        _cauHinh = cauHinh.Value;
    }

    /// <summary>Gợi ý người thực hiện phù hợp cho một nhiệm vụ.</summary>
    /// <remarks>
    /// Gọi được theo hai cách:
    /// <list type="bullet">
    ///   <item>Đưa <c>taskId</c> của nhiệm vụ đã lưu.</item>
    ///   <item>Đưa thẳng <c>title</c> và <c>description</c> khi đang gõ, chưa bấm lưu —
    ///         để xem gợi ý ngay trên màn tạo nhiệm vụ.</item>
    /// </list>
    /// Mỗi ứng viên trả về kèm điểm bốn thành phần, số liệu thô và lý do tiếng Việt.
    /// </remarks>
    [HttpPost("nguoi-thuc-hien")]
    [ProducesResponseType(typeof(GoiYResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GoiYNguoiThucHien(
        [FromBody] GoiYRequest yeuCau, CancellationToken ct)
    {
        var kq = await _service.GoiYAsync(yeuCau, _hienTai.LayUserIdBatBuoc(), ct);

        if (!kq.ThanhCong)
        {
            var maHttp = kq.MaLoi switch
            {
                MaLoiChung.KhongCoQuyen => StatusCodes.Status403Forbidden,
                MaLoiChung.KhongTimThay => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return Problem(detail: kq.Loi, statusCode: maHttp, title: "Không gợi ý được");
        }

        // Để frontend hoặc người kiểm thử thấy ngay thời gian tính toán mà không cần đọc log.
        Response.Headers["X-Thoi-Gian-Ms"] = kq.DuLieu!.ThoiGianMs.ToString();
        return Ok(kq.DuLieu);
    }

    /// <summary>
    /// Xem bộ trọng số đang dùng. Phục vụ việc giải thích mô hình khi trình bày kết quả.
    /// </summary>
    [HttpGet("cau-hinh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult XemCauHinh() => Ok(new
    {
        phienBan = _cauHinh.PhienBan,
        trongSo = new
        {
            kyNang = _cauHinh.TrongSoKyNang,
            kinhNghiem = _cauHinh.TrongSoKinhNghiem,
            dungHan = _cauHinh.TrongSoDungHan,
            khoiLuong = _cauHinh.TrongSoKhoiLuong,
            tong = _cauHinh.TongTrongSo
        },
        thamSo = new
        {
            nguongKinhNghiem = _cauHinh.NguongKinhNghiem,
            nguongKhoiLuong = _cauHinh.NguongKhoiLuong,
            tyLeDungHanTienNghiem = _cauHinh.TyLeDungHanTienNghiem,
            soQuanSatAo = _cauHinh.SoQuanSatAo
        },
        giaiThich = new
        {
            kyNang = "TF-IDF + cosine giữa nội dung nhiệm vụ và hồ sơ kỹ năng đã khai.",
            kinhNghiem = "Số nhiệm vụ đã hoàn thành, thang log để người làm nhiều không áp đảo tuyệt đối.",
            dungHan = "Tỷ lệ hoàn thành trước hạn, làm mượt Laplace để người ít dữ liệu không bị điểm cực đoan.",
            khoiLuong = "Càng ít việc đang gánh thì điểm càng cao, nhằm san đều khối lượng."
        }
    });
}
