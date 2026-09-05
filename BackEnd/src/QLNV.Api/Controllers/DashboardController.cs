using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.10 J1-J2 / §3.1 M02 — bang dieu khien.
///
/// PHAM VI DU LIEU (§6.2 dong 20): QUAN_TRI xem toan he thong; NGUOI_GIAO chi xem
/// don vi minh va don vi con; NGUOI_THUC_HIEN khong duoc xem bang dieu khien toan
/// he thong, chi thay phan "viec cua toi".
///
/// Viec cat pham vi nam o <see cref="IThongKeService"/> (nhan <c>userId</c> lam dau vao)
/// vi chi tang do moi biet cay don vi; tang API chi lam lop 1 cua §6.4 — chan theo vai tro.
///
/// §1.4: KHONG dung job nen thong ke va truc TRANGTHAITHONGKE cua he goc; moi con so
/// tinh truc tiep tu trang thai nghiep vu + han xu ly.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IThongKeService _thongKe;
    private readonly IHienTai _hienTai;

    public DashboardController(IThongKeService thongKe, IHienTai hienTai)
    {
        _thongKe = thongKe;
        _hienTai = hienTai;
    }

    /// <summary>
    /// J1 — Tong quan bang dieu khien: 4 the dem, bieu do tron theo trang thai,
    /// bieu do cot theo don vi va danh sach viec sap den han (§5.10 J1, §3.1 M02).
    /// </summary>
    /// <remarks>
    /// Cac the dem: chua trien khai (trang thai 3), dang trien khai (2), cho xac nhan
    /// (truc B = 10), qua han (7 hoac hanxulyth &lt; hom nay), sap het han
    /// (con lai tu 0 den 3 ngay — §5.10 J1), hoan thanh (1 hoac 5).
    ///
    /// Pham vi du lieu tuan thu §6.2 dong 20 va duoc cat ben trong IThongKeService theo
    /// <c>userId</c> cua nguoi dang dang nhap:
    ///  - QUAN_TRI: toan he thong;
    ///  - NGUOI_GIAO: don vi minh + don vi con;
    ///  - NGUOI_THUC_HIEN: chi cac nhiem vu minh co ten trong NHIEMVU_PHANCONG.
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">So lieu tong quan.</response>
    /// <response code="401">Chua dang nhap.</response>
    [HttpGet("tong-quan")]
    [ProducesResponseType(typeof(DashboardTongQuanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TongQuan(CancellationToken ct)
    {
        var userId = _hienTai.UserId;
        if (!_hienTai.DaDangNhap || userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        var ketQua = await _thongKe.TongQuanAsync(userId.Value, ct);
        return Ok(ketQua);
    }

    /// <summary>
    /// J2 — So nhiem vu va ty le hoan thanh theo tung don vi (§5.10 J2).
    /// </summary>
    /// <remarks>
    /// §6.2 dong 20 danh dau NGUOI_THUC_HIEN la KHONG duoc xem bang dieu khien toan he
    /// thong, nen endpoint nay chi mo cho QUAN_TRI va NGUOI_GIAO. Nguoi giao chi nhan ve
    /// don vi minh va cac don vi con — do IThongKeService cat theo <c>userId</c>.
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Mot dong cho moi don vi trong pham vi duoc xem.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Nguoi thuc hien khong duoc xem thong ke theo don vi (§6.2 dong 20).</response>
    [HttpGet("theo-don-vi")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardDonViDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TheoDonVi(CancellationToken ct)
    {
        var userId = _hienTai.UserId;
        if (!_hienTai.DaDangNhap || userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        // §6.2 dong 20 — chi QUAN_TRI (toan he thong) va NGUOI_GIAO (don vi minh + don vi con).
        if (!VaiTro.LaBenGiao(_hienTai.VaiTro))
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Bạn không có quyền xem thống kê theo đơn vị.",
                MaLoiChung.KhongCoQuyen);
        }

        var ketQua = await _thongKe.TheoDonViAsync(userId.Value, ct);
        return Ok(ketQua);
    }

    /// <summary>
    /// M02 (bo sung) — "Việc của tôi sắp đến hạn": danh sach nhiem vu con lai duoi
    /// <paramref name="soNgay"/> ngay, ke ca cac nhiem vu da qua han.
    /// </summary>
    /// <remarks>
    /// Mo cho moi vai tro vi day la viec cua chinh nguoi dang dang nhap, khong phai
    /// bang dieu khien toan he thong o §6.2 dong 20.
    /// </remarks>
    /// <param name="soNgay">Nguong so ngay con lai; mac dinh 3 (§5.10 J1, GioiHan.NguongSapHetHan).</param>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Danh sach nhiem vu sap den han.</response>
    /// <response code="400">Nguong so ngay khong hop le.</response>
    /// <response code="401">Chua dang nhap.</response>
    [HttpGet("sap-den-han")]
    [ProducesResponseType(typeof(IReadOnlyList<NhiemVuSapDenHanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SapDenHan(
        CancellationToken ct,
        [FromQuery] int soNgay = GioiHan.NguongSapHetHan)
    {
        var userId = _hienTai.UserId;
        if (!_hienTai.DaDangNhap || userId is null)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        // QUYET DINH CUA TANG API (dac ta khong noi): chan nguong am va nguong qua lon
        // de tranh bien "sap den han" thanh "toan bo nhiem vu".
        if (soNgay < 0 || soNgay > 365)
        {
            return Loi(StatusCodes.Status400BadRequest,
                "Ngưỡng số ngày phải nằm trong khoảng từ 0 đến 365.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = await _thongKe.SapDenHanAsync(userId.Value, soNgay, ct);
        return Ok(ketQua);
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
