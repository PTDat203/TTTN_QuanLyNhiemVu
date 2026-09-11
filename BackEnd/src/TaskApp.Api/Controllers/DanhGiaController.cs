using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Api.Common;
using TaskApp.Api.Services.GoiY;

namespace TaskApp.Api.Controllers;

/// <summary>
/// Hiệu chỉnh và đánh giá mô hình gợi ý trên dữ liệu lịch sử — phục vụ chương đánh giá của báo cáo.
///
/// <para>
/// Chỉ Giám đốc gọi được: các endpoint này chạy lại mô hình trên toàn bộ lịch sử, tốn thời gian và
/// trả về dữ liệu của mọi phòng ban.
/// </para>
/// <para>Thứ tự dùng: <c>phan-bo</c> → đặt cặp ngưỡng chuẩn hoá → <c>do-nguong</c> → đặt ngưỡng
/// quyết định → <c>chay</c>.</para>
/// </summary>
[ApiController]
[Route("api/danh-gia")]
[Authorize(Roles = VaiTro.GiamDoc)]
[Produces("application/json")]
public sealed class DanhGiaController : ControllerBase
{
    private readonly DanhGiaGoiY _danhGia;

    public DanhGiaController(DanhGiaGoiY danhGia) => _danhGia = danhGia;

    /// <summary>Phân bố độ tương đồng thô của cặp liên quan / không liên quan, kèm AUC và cặp ngưỡng đề xuất.</summary>
    [HttpGet("phan-bo")]
    public async Task<IActionResult> PhanBo(CancellationToken ct) => Ok(await _danhGia.PhanBoAsync(ct));

    /// <summary>Dò ngưỡng phòng ban và ngưỡng trích kỹ năng trên tập hiệu chỉnh.</summary>
    /// <param name="phuongPhap"><c>nhung</c> (mặc định) hoặc <c>tfidf</c>.</param>
    /// <param name="ct">Huỷ giữa chừng khi người gọi ngắt kết nối.</param>
    [HttpGet("do-nguong")]
    public async Task<IActionResult> DoNguong([FromQuery] string phuongPhap = "nhung", CancellationToken ct = default)
        => Ok(await _danhGia.DoNguongAsync(
            string.Equals(phuongPhap, "tfidf", StringComparison.OrdinalIgnoreCase), ct));

    /// <summary>So ba phương pháp — theo luật, TF-IDF, nhúng ngữ nghĩa — trên tập kiểm tra.</summary>
    [HttpGet("chay")]
    public async Task<IActionResult> Chay(CancellationToken ct) => Ok(await _danhGia.DanhGiaAsync(ct));
}
