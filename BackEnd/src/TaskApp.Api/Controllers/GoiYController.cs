using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskApp.Api.Auth;
using TaskApp.Api.Common;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services;
using TaskApp.Api.Services.Ai;

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
[Authorize(Roles = VaiTro.NhomGiaoViec)]
[Produces("application/json")]
public sealed class GoiYController : ControllerBase
{
    private readonly GoiYService _service;
    private readonly NguoiDungHienTai _hienTai;
    private readonly CauHinhGoiY _cauHinh;
    private readonly DichVuNhung _nhung;

    public GoiYController(
        GoiYService service, NguoiDungHienTai hienTai, IOptions<CauHinhGoiY> cauHinh, DichVuNhung nhung)
    {
        _service = service;
        _hienTai = hienTai;
        _cauHinh = cauHinh.Value;
        _nhung = nhung;
    }

    /// <summary>Gợi ý người thực hiện phù hợp cho một nhiệm vụ.</summary>
    /// <remarks>
    /// Gọi được theo hai cách:
    /// <list type="bullet">
    ///   <item>Đưa <c>taskId</c> của nhiệm vụ đã lưu.</item>
    ///   <item>Đưa thẳng <c>title</c> và <c>description</c> khi đang gõ, chưa bấm lưu —
    ///         để xem gợi ý ngay trên màn tạo nhiệm vụ.</item>
    /// </list>
    /// Kết quả gồm: AI đoán nhiệm vụ thuộc phòng nào, cần kỹ năng gì; rồi danh sách ứng viên, mỗi
    /// người kèm điểm sáu thành phần, số liệu thô và lý do tiếng Việt.
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
    /// Xem bộ tham số đang dùng và trạng thái dịch vụ AI. Phục vụ việc giải thích mô hình khi
    /// trình bày kết quả, và để giao diện biết đang chạy bằng mô hình nhúng hay TF-IDF.
    /// </summary>
    [HttpGet("cau-hinh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> XemCauHinh(CancellationToken ct)
    {
        var (sanSang, moHinh, loi) = await _nhung.KiemTraAsync(ct);
        var c = _cauHinh;

        return Ok(new
        {
            phienBan = c.PhienBan,
            dichVuNhung = new { sanSang, moHinh, loi, soVecToDangDem = _nhung.SoMucDangDem },
            trongSo = new
            {
                nguNghia = c.TrongSoNguNghia,
                mucKyNang = c.TrongSoMucKyNang,
                hieuSuat = c.TrongSoHieuSuat,
                viecTuongTu = c.TrongSoViecTuongTu,
                dungHan = c.TrongSoDungHan,
                khoiLuong = c.TrongSoKhoiLuong,
                tong = c.TongTrongSo
            },
            thamSo = new
            {
                hieuSuatTienNghiem = c.HieuSuatTienNghiem,
                tyLeDungHanTienNghiem = c.TyLeDungHanTienNghiem,
                soQuanSatAo = c.SoQuanSatAo,
                nguongKhoiLuong = c.NguongKhoiLuong,
                soViecTuongTu = c.SoViecTuongTu,
                mucKyNangMacDinh = c.MucKyNangMacDinh
            },
            hieuChinh = new { nhung = c.Nhung, tfIdf = c.TfIdf },
            giaiThich = new
            {
                nguNghia = "Độ gần nghĩa giữa nội dung nhiệm vụ và hồ sơ người (chức danh, kỹ năng, học vấn), đo bằng mô hình nhúng đa ngữ.",
                mucKyNang = "Có đủ mức ở những kỹ năng nhiệm vụ đòi hỏi không. Không có kỹ năng thì 0, đủ mức thì 1.",
                hieuSuat = "Điểm đánh giá chất lượng các việc đã hoàn thành, làm mượt Laplace để người ít dữ liệu không bị điểm cực đoan.",
                viecTuongTu = "Đã làm những việc giống việc này chưa, và làm tốt tới đâu. Chưa làm việc nào giống thì trung tính.",
                dungHan = "Tỷ lệ hoàn thành trước hạn, làm mượt Laplace.",
                khoiLuong = "Càng ít việc đang gánh thì điểm càng cao, để việc không dồn hết vào người giỏi nhất."
            }
        });
    }
}
