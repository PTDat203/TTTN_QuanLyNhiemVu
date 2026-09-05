using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Dtos;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.9 I4-I5 — kich hoat THU CONG cac job nen. Binh thuong hai job nay chay theo cron
/// (I4 luc 01:00, I5 dau ngay); endpoint o day de van hanh chay lai khi can va de nguoi
/// lam de tai demo duoc trong buoi bao ve.
///
/// PHAN QUYEN: chi QUAN_TRI. Ca hai job deu ghi de du lieu tren dien rong (I5 doi
/// trang thai nhiem vu 2 -&gt; 7 va 3 -&gt; 7) nen khong mo cho NGUOI_GIAO.
///
/// Nghiep vu nam hoan toan o <see cref="IThongKeService"/> ben Infrastructure; tang API
/// chi chan quyen, chuan hoa tham so va tra ket qua.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/jobs")]
[Produces("application/json")]
public sealed class JobsController : ControllerBase
{
    private readonly IThongKeService _thongKe;
    private readonly IHienTai _hienTai;
    private readonly ILogger<JobsController> _log;

    public JobsController(IThongKeService thongKe, IHienTai hienTai, ILogger<JobsController> log)
    {
        _thongKe = thongKe;
        _hienTai = hienTai;
        _log = log;
    }

    /// <summary>
    /// I4 — Chay job tong hop bang hieu suat nguoi dung <c>USER_HIEUSUAT</c> (§5.9 I4, §4.8).
    /// </summary>
    /// <remarks>
    /// Job tinh lai cac cot tong hop phuc vu AI cham diem (§9.4): so nhiem vu hoan thanh,
    /// so nhiem vu dung han, so lan bi tra lai, so lan gia han, tai trong so dang giu,
    /// so nhiem vu qua han. Thuong chay cron luc 01:00; endpoint nay de chay lai thu cong.
    ///
    /// Job la idempotent: chay nhieu lan trong ngay cho cung ket qua.
    ///
    /// Vi du ket qua 200:
    ///
    ///     {
    ///       "job": "CAP_NHAT_HIEU_SUAT",
    ///       "soBanGhi": 22,
    ///       "batDau": "2026-09-05T01:00:00",
    ///       "ketThuc": "2026-09-05T01:00:02",
    ///       "thongBao": "Đã tổng hợp hiệu suất cho 22 người dùng."
    ///     }
    ///
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Ket qua chay job.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc kich hoat job.</response>
    [HttpPost("cap-nhat-hieu-suat")]
    [ProducesResponseType(typeof(KetQuaJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CapNhatHieuSuat(CancellationToken ct)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        var ketQua = await _thongKe.ChayJobCapNhatHieuSuatAsync(ct);

        _log.LogInformation("Chay thu cong job {Job}: {SoBanGhi} ban ghi, boi userId={UserId}",
            ketQua.Job, ketQua.SoBanGhi, _hienTai.UserId);

        return Ok(ketQua);
    }

    /// <summary>
    /// I5 — Chay job danh dau qua han: <c>trangthai 2 -&gt; 7</c> va <c>3 -&gt; 7</c>
    /// khi <c>hanxulyth</c> nho hon ngay chay (§5.9 I5, §2.5).
    /// </summary>
    /// <remarks>
    /// §2.5 — quy tac tu dong theo han. Job KHONG dung dao vao cac trang thai ket thuc
    /// {1, 5, 97}, khong dung dao vao nhiem vu dang cho duyet gia han (13) va khong dao
    /// nguoc 7 -&gt; 2 khi han duoc noi ra; viec khoi phuc trang thai theo han moi thuoc ve
    /// buoc duyet gia han T12 (§2.4).
    ///
    /// Tham so <c>homNay</c> chi de kiem thu va demo. Mac dinh la ngay he thong.
    /// KHONG cho truyen ngay o TUONG LAI: lam vay se danh dau qua han hang loat nhiem vu
    /// van con han, va §2.5 khong co duong quay lai.
    ///
    /// Vi du:
    ///
    ///     POST /api/v1/jobs/cap-nhat-qua-han
    ///     POST /api/v1/jobs/cap-nhat-qua-han?homNay=2026-09-05
    ///
    /// Vi du ket qua 200:
    ///
    ///     {
    ///       "job": "CAP_NHAT_QUA_HAN",
    ///       "soBanGhi": 7,
    ///       "batDau": "2026-09-05T00:05:00",
    ///       "ketThuc": "2026-09-05T00:05:01",
    ///       "thongBao": "Đã chuyển 7 nhiệm vụ sang trạng thái Đang triển khai quá hạn."
    ///     }
    ///
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <param name="homNay">Ngay chay job. Bo trong = ngay he thong. Khong duoc o tuong lai.</param>
    /// <response code="200">Ket qua chay job.</response>
    /// <response code="400">Ngay chay job o tuong lai.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc kich hoat job.</response>
    [HttpPost("cap-nhat-qua-han")]
    [ProducesResponseType(typeof(KetQuaJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CapNhatQuaHan(CancellationToken ct, [FromQuery] DateOnly? homNay = null)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        var ngayHeThong = NgayUtil.HomNay();
        var ngayChay = homNay ?? ngayHeThong;

        // QUYET DINH CUA TANG API (dac ta khong noi): chan ngay tuong lai — §2.5 chi co
        // chieu 2/3 -> 7, khong co duong quay lai neu danh dau nham.
        if (ngayChay > ngayHeThong)
        {
            return Loi(StatusCodes.Status400BadRequest,
                "Không được chạy job quá hạn với ngày ở tương lai.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = await _thongKe.ChayJobCapNhatQuaHanAsync(ngayChay, ct);

        _log.LogInformation("Chay thu cong job {Job} ngay {NgayChay}: {SoBanGhi} ban ghi, boi userId={UserId}",
            ketQua.Job, ngayChay, ketQua.SoBanGhi, _hienTai.UserId);

        return Ok(ketQua);
    }

    /// <summary>Chi QUAN_TRI duoc kich hoat job. Tra null khi duoc phep di tiep.</summary>
    private ObjectResult? ChanNeuKhongPhaiQuanTri()
    {
        if (!_hienTai.DaDangNhap)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        if (!_hienTai.LaQuanTri)
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Chỉ quản trị hệ thống mới được kích hoạt job nền.",
                MaLoiChung.KhongCoQuyen);
        }

        return null;
    }

    /// <summary>
    /// Dung mot phan hoi loi chuan (ProblemDetails) kem khoa "loi" va "maLoi".
    /// Thong bao LUON tieng Viet co dau.
    /// </summary>
    private ObjectResult Loi(int maHttp, string thongBao, string maLoi)
    {
        var chiTiet = new ProblemDetails
        {
            Status = maHttp,
            Title = thongBao,
            Detail = thongBao,
            Instance = HttpContext?.Request.Path.Value
        };
        chiTiet.Extensions["loi"] = thongBao;
        chiTiet.Extensions["maLoi"] = maLoi;
        return new ObjectResult(chiTiet) { StatusCode = maHttp };
    }
}
