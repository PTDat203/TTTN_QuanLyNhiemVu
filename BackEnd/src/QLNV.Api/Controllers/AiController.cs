using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.8 — nhom API AI goi y nguoi thuc hien (H1..H4) + so sanh baseline va nhat ky (§9.8, M13).
///
/// PHAN QUYEN (§6.2):
///  - dong 5  "Dung AI goi y nguoi thuc hien": QUAN_TRI + NGUOI_GIAO  =&gt; H1, H2.
///  - dong 24 "Xem nhat ky va tinh chinh trong so AI": CHI QUAN_TRI    =&gt; H3, H4,
///    so-sanh-baseline, nhat-ky.
///
/// Tang nay KHONG chua nghiep vu cham diem — toan bo nam o <see cref="IRecommendationService"/>
/// (§9.3 loc cung, §9.4 cong thuc S1..S5, §9.5 cold start, §9.6 sinh ly do).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/ai")]
[Produces("application/json")]
public sealed class AiController : ControllerBase
{
    /// <summary>Ten header tra ve thoi gian xu ly H1 (dac ta ky vong duoi 50 ms).</summary>
    public const string HeaderThoiGian = "X-Thoi-Gian-Ms";

    private readonly IRecommendationService _goiY;
    private readonly IMetricsService _doLuong;
    private readonly IHienTai _hienTai;
    private readonly ILogger<AiController> _log;

    public AiController(
        IRecommendationService goiY,
        IMetricsService doLuong,
        IHienTai hienTai,
        ILogger<AiController> log)
    {
        _goiY = goiY;
        _doLuong = doLuong;
        _hienTai = hienTai;
        _log = log;
    }

    // ------------------------------------------------------------------
    // H1 — endpoint trong tam cua de tai
    // ------------------------------------------------------------------

    /// <summary>
    /// H1 — Goi y danh sach nguoi thuc hien phu hop cho mot nhiem vu (§5.8, §9.6).
    /// </summary>
    /// <remarks>
    /// Diem nhan cua de tai. Mo hinh la cham diem da tieu chi co trong so (MCDM / rule-based),
    /// khong phai mang no-ron — xem §9.9 "Trung thuc ve thuat ngu". Moi ung vien tra kem
    /// 5 diem thanh phan va toi da 4 dong ly do tieng Viet nen nguoi giao giai thich duoc
    /// vi sao he thong xep hang nhu vay.
    ///
    /// Thu tu xu ly ben trong (§9): loc cung §9.3 -&gt; cham S1..S5 §9.4 -&gt; xac dinh che do
    /// §9.5 -&gt; sinh nhan va ly do §9.6 -&gt; sap xep (nguoi QUA_TAI luon o cuoi) -&gt; cat theo
    /// soLuong. Ham co ghi mot ban ghi AI_GOIY_LOG de sau con do ty le chap nhan (§9.8).
    ///
    /// Header tra ve X-Thoi-Gian-Ms — thoi gian xu ly phia may chu, tinh bang mili giay.
    /// Dac ta ky vong duoi 50 ms voi quy mo du lieu cua de tai.
    ///
    /// Vi du body (§9.6):
    ///
    ///     POST /api/v1/ai/goi-y-nguoi-thuc-hien
    ///     {
    ///       "noidung": "Ra soat va bao cao tinh hinh trien khai he thong mot cua dien tu quy III",
    ///       "linhvuc": "CNTT",
    ///       "dokhan": "TRONGTAM",
    ///       "hanxulyth": "2026-10-15",
    ///       "phamViUnitCode": ["P01", "P02"],
    ///       "loaiTru": ["3f1b2c40-0000-0000-0000-000000000001"],
    ///       "soLuong": 5
    ///     }
    ///
    /// Vi du ket qua 200 (rut gon con 1 ung vien):
    ///
    ///     {
    ///       "goiyId": "b1f2c3d4-0000-0000-0000-0000000000aa",
    ///       "phienBanTrongSo": "v1.0",
    ///       "cheDo": "DAY_DU",
    ///       "canhBao": [],
    ///       "ungVien": [
    ///         {
    ///           "userid": "3f1b2c40-0000-0000-0000-000000000009",
    ///           "fullname": "Nguyen Van A",
    ///           "chucvu": "Chuyen vien",
    ///           "unitname": "Phong CNTT",
    ///           "diemTong": 87.4,
    ///           "doTinCay": 0.92,
    ///           "nhan": [],
    ///           "diemThanhPhan": {
    ///             "chuyenMon": { "diem": 1.00, "trongSo": 0.30, "gopPhan": 30.0 },
    ///             "lichSu":    { "diem": 0.86, "trongSo": 0.20, "gopPhan": 17.2 },
    ///             "hieuQua":   { "diem": 0.88, "trongSo": 0.25, "gopPhan": 22.0 },
    ///             "khoiLuong": { "diem": 0.75, "trongSo": 0.20, "gopPhan": 15.0 },
    ///             "sanSang":   { "diem": 0.65, "trongSo": 0.05, "gopPhan":  3.2 }
    ///           },
    ///           "lyDo": [
    ///             "Da hoan thanh 7 nhiem vu cung linh vuc",
    ///             "Ty le dung han 86% (6/7)",
    ///             "Dang giu 2 nhiem vu (tai 2,0/8 - con nhieu du dia)"
    ///           ],
    ///           "soLieu": {
    ///             "soNvHoanThanhLinhVuc": 7, "soNvHoanThanh": 12, "soNvDungHan": 10,
    ///             "soNvBiTraLai": 1, "soLanGiaHan": 0, "soNvDangMo": 2,
    ///             "taiTrongSo": 2.0, "soNvQuaHan": 0, "K": 8, "tyLeDungHan": 0.83,
    ///             "mucThanhThao": null, "diemChatLuongTb": null, "soNvDuocGiao": 14
    ///           }
    ///         }
    ///       ]
    ///     }
    ///
    /// Bao dam cua §9.5: khong bao gio tra danh sach rong khi con ung vien qua duoc bo loc
    /// cung; neu moi nguoi deu 0 diem thi xep theo S4 (ai ranh nhat) va ghi ro ly do vao canhBao.
    /// </remarks>
    /// <param name="req">Ngu canh nhiem vu can goi y (§9.6).</param>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Danh sach ung vien da xep hang, kem diem thanh phan va ly do.</response>
    /// <response code="400">Du lieu dau vao khong hop le (do khan sai ma, so luong vuot nguong).</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Vai tro khong duoc dung AI goi y (§6.2 dong 5).</response>
    [HttpPost("goi-y-nguoi-thuc-hien")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GoiYResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GoiYNguoiThucHien([FromBody] GoiYRequest req, CancellationToken ct)
    {
        // §6.4 lop 1 — kiem vai tro truoc khi cham vao du lieu.
        if (!_hienTai.DaDangNhap)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        // §6.2 dong 5 — chi QUAN_TRI va NGUOI_GIAO duoc mo popup goi y.
        if (!VaiTro.LaBenGiao(_hienTai.VaiTro))
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Chỉ người giao nhiệm vụ hoặc quản trị mới được dùng chức năng AI gợi ý người thực hiện.",
                MaLoiChung.KhongCoQuyen);
        }

        // §4.1 / §4.2 — do khan chi nhan 3 ma; sai ma se lam lech trong so tai (§9.4 S4).
        if (!string.IsNullOrWhiteSpace(req.DoKhan) && !DoKhan.HopLe(req.DoKhan))
        {
            return Loi(StatusCodes.Status400BadRequest,
                "Độ khẩn không hợp lệ. Chỉ nhận TRONGTAM, THUONGXUYEN hoặc DOTXUAT.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // QUYET DINH CUA TANG API (dac ta khong noi): soLuong <= 0 thi dung mac dinh 5;
        // vuot GioiHan.SoUngVienToiDa thi bao loi thay vi am tham cat, de nguoi goi biet.
        if (req.SoLuong <= 0)
        {
            req.SoLuong = GioiHan.SoUngVienMacDinh;
        }

        if (req.SoLuong > GioiHan.SoUngVienToiDa)
        {
            return Loi(StatusCodes.Status400BadRequest,
                $"Số lượng ứng viên tối đa là {GioiHan.SoUngVienToiDa}.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // §9.6 — dac ta ky vong H1 chay duoi 50 ms; do va tra ve de con dua vao bao cao.
        var dongHo = Stopwatch.StartNew();
        var ketQua = await _goiY.GoiYAsync(req, ct);
        dongHo.Stop();

        Response.Headers[HeaderThoiGian] =
            dongHo.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);

        _log.LogInformation(
            "AI goi y: goiyId={GoiYId}, linhVuc={LinhVuc}, cheDo={CheDo}, soUngVien={SoUngVien}, thoiGianMs={ThoiGianMs}",
            ketQua.GoiYId, ketQua.LinhVuc, ketQua.CheDo, ketQua.UngVien.Count, dongHo.Elapsed.TotalMilliseconds);

        return Ok(ketQua);
    }

    // ------------------------------------------------------------------
    // H2 — ghi nhan nguoi giao da chon ai
    // ------------------------------------------------------------------

    /// <summary>
    /// H2 — Ghi nhan nguoi giao thuc te da chon ai sau khi xem goi y (§5.8).
    /// </summary>
    /// <remarks>
    /// Cap nhat userid_da_chon, thu_hang_da_chon va co_trong_goi_y cua ban ghi AI_GOIY_LOG.
    /// Day la nguon duy nhat de tinh Precision@1 / @3, MRR va ty le chap nhan o §9.8 — FE
    /// phai goi ca khi nguoi giao BO QUA goi y (gui useridDaChon = null), neu khong mau so
    /// cua cac chi so se sai.
    ///
    /// Rang buoc da kiem chung: co_trong_goi_y chot cung o top-5, khong phu thuoc soLuong
    /// cua lan goi H1 — nho vay bam "Xem thêm 5 người" khong lam meo so lieu giua cac lan goi.
    ///
    /// Vi du body:
    ///
    ///     { "useridDaChon": "3f1b2c40-0000-0000-0000-000000000009" }
    ///
    /// Bo qua goi y, chon thu cong:
    ///
    ///     { "useridDaChon": null }
    ///
    /// </remarks>
    /// <param name="goiyId">Ma lan goi y do H1 tra ve (AI_GOIY_LOG.id).</param>
    /// <param name="req">Nguoi duoc chon; null = bo qua goi y, chon thu cong.</param>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="204">Da ghi nhan.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Vai tro khong duoc dung AI goi y (§6.2 dong 5).</response>
    [HttpPost("goi-y/{goiyId:guid}/ket-qua")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GhiKetQuaGoiY(
        Guid goiyId,
        [FromBody] GhiKetQuaGoiYRequest? req,
        CancellationToken ct)
    {
        if (!_hienTai.DaDangNhap)
        {
            return Loi(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.", MaLoiChung.ChuaDangNhap);
        }

        // §6.2 dong 5 — cung nhom quyen voi H1.
        if (!VaiTro.LaBenGiao(_hienTai.VaiTro))
        {
            return Loi(StatusCodes.Status403Forbidden,
                "Chỉ người giao nhiệm vụ hoặc quản trị mới được ghi nhận kết quả gợi ý.",
                MaLoiChung.KhongCoQuyen);
        }

        // Body rong = bo qua goi y (chon thu cong) — van phai ghi nhan de §9.8 dem dung mau so.
        await _goiY.GhiKetQuaAsync(goiyId, req?.UserIdDaChon, ct);
        return NoContent();
    }

    // ------------------------------------------------------------------
    // H3 — do hieu qua (§9.8)
    // ------------------------------------------------------------------

    /// <summary>
    /// H3 — So lieu do hieu qua AI: Precision@1, Precision@3, MRR, ty le chap nhan,
    /// phan bo thu hang, phan bo diem va he so Gini do lech tai (§5.8, §9.8).
    /// </summary>
    /// <remarks>
    /// Tat ca tinh tu bang AI_GOIY_LOG. Quy uoc da kiem chung, phai giu: mau so cua ca bon
    /// chi so la TONG SO LAN GOI Y (moi lan mo popup), khong phai so lan co nguoi duoc chon;
    /// lan bo qua goi y dong gop 0 vao MRR. Truong "dat" so bang gia tri tho, khong dung
    /// gia tri da lam tron.
    ///
    /// Nguong muc tieu §9.8: Precision@1 &gt;= 0,35 · Precision@3 &gt;= 0,60 ·
    /// ty le chap nhan &gt;= 0,70 · MRR &gt;= 0,50 · Gini tai phai giam.
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Bang chi so cho man M13 va bao cao thuc tap.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc xem (§6.2 dong 24).</response>
    [HttpGet("thong-ke")]
    [ProducesResponseType(typeof(ThongKeAiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ThongKe(CancellationToken ct)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        var ketQua = await _doLuong.ThongKeAiAsync(ct);
        return Ok(ketQua);
    }

    /// <summary>
    /// §9.8 — Bang so sanh BAT BUOC co trong bao cao: mo hinh cham diem day du so voi
    /// 3 baseline (chon ngau nhien, chon nguoi ranh nhat, chon nguoi chuyen mon cao nhat),
    /// doi chieu tren Precision@1, Precision@3, MRR va Gini tai.
    /// </summary>
    /// <remarks>
    /// Bo sung ngoai bang §5.8: §9.8 quy dinh bang so sanh nay la bat buoc trong bao cao,
    /// neu khong co endpoint rieng thi man M13 khong lay duoc so lieu.
    /// Ket qua ky vong: mo hinh day du tot hon ca 3 baseline o Precision@3 va do dong deu tai.
    /// </remarks>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Mot dong cho moi chien luoc.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc xem (§6.2 dong 24).</response>
    [HttpGet("so-sanh-baseline")]
    [ProducesResponseType(typeof(IReadOnlyList<SoSanhBaselineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SoSanhBaseline(CancellationToken ct)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        var ketQua = await _doLuong.SoSanhBaselineAsync(ct);
        return Ok(ketQua);
    }

    /// <summary>
    /// M13 — Nhat ky goi y AI, phan trang (§6.2 dong 24).
    /// </summary>
    /// <param name="trang">So hieu trang, bat dau tu 1.</param>
    /// <param name="kichThuoc">So ban ghi moi trang; toi da 200.</param>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Trang nhat ky.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc xem (§6.2 dong 24).</response>
    [HttpGet("nhat-ky")]
    [ProducesResponseType(typeof(PagedResult<AiGoiYLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> NhatKy(
        CancellationToken ct,
        [FromQuery] int trang = 1,
        [FromQuery] int kichThuoc = GioiHan.KichThuocTrangMacDinh)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        if (trang < 1)
        {
            trang = 1;
        }

        if (kichThuoc < 1)
        {
            kichThuoc = GioiHan.KichThuocTrangMacDinh;
        }

        if (kichThuoc > GioiHan.KichThuocTrangToiDa)
        {
            kichThuoc = GioiHan.KichThuocTrangToiDa;
        }

        var ketQua = await _doLuong.NhatKyAsync(trang, kichThuoc, ct);
        return Ok(ketQua);
    }

    // ------------------------------------------------------------------
    // H4 — bo trong so
    // ------------------------------------------------------------------

    /// <summary>
    /// H4 (GET) — Doc bo trong so w1..w5, cac nguong va hang so lam muot dang hieu luc
    /// cua mo hinh cham diem (§5.8, §9.4).
    /// </summary>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Bo cau hinh hien tai.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc xem (§6.2 dong 24).</response>
    [HttpGet("cau-hinh")]
    [ProducesResponseType(typeof(CauHinhAiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DocCauHinh(CancellationToken ct)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        var cauHinh = await _goiY.DocCauHinhAsync(ct);
        return Ok(cauHinh);
    }

    /// <summary>
    /// H4 (PUT) — Cap nhat bo trong so cua mo hinh cham diem (§5.8, §9.2, §9.4).
    /// </summary>
    /// <remarks>
    /// Rang buoc bat buoc: w1 + w2 + w3 + w4 + w5 = 1,00 (§9.2). Sai lech cho phep la 0,001
    /// de tranh loi lam tron dau phay dong. Neu tong khac 1 thi tra 400 kem thong bao tieng
    /// Viet ghi ro tong hien tai — KHONG am tham chuan hoa ho, vi lam vay se khien phan ra
    /// diemThanhPhan tren M06 khong con cong dung ra diemTong.
    ///
    /// Vi du body (bo mac dinh §9.2):
    ///
    ///     PUT /api/v1/ai/cau-hinh
    ///     {
    ///       "w1": 0.30, "w2": 0.20, "w3": 0.25, "w4": 0.20, "w5": 0.05,
    ///       "nguongChuyenMon": 5, "nguongLichSu": 10,
    ///       "p0": 0.70, "alpha": 5,
    ///       "phatTraLai": 0.30, "phatGiaHan": 0.15,
    ///       "heSoQuaTai": 1.5,
    ///       "phienBan": "v1.0"
    ///     }
    ///
    /// </remarks>
    /// <param name="cauHinh">Bo trong so va hang so moi.</param>
    /// <param name="ct">Token huy cua request.</param>
    /// <response code="200">Da luu; tra lai bo cau hinh sau khi luu.</response>
    /// <response code="400">Tong trong so khac 1,00 hoac nguong khong hop le.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc sua (§6.2 dong 24).</response>
    [HttpPut("cau-hinh")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CauHinhAiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LuuCauHinh([FromBody] CauHinhAiDto cauHinh, CancellationToken ct)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        // §9.2 — chan ngay o tang API cho thong bao that ro rang, truoc khi hoi tang duoi.
        var tong = cauHinh.TongTrongSo;
        if (Math.Abs(tong - 1.0) > 0.001)
        {
            var tongChu = tong.ToString("0.###", CultureInfo.InvariantCulture);
            return Loi(StatusCodes.Status400BadRequest,
                $"Tổng năm trọng số w1..w5 phải bằng 1,00 nhưng hiện là {tongChu}. Vui lòng điều chỉnh lại trước khi lưu.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // Kiem tra sau cua tang nghiep vu (cac nguong, hang so lam muot...).
        var kiemTra = _goiY.KiemTraCauHinh(cauHinh);
        if (!kiemTra.HopLe)
        {
            var chiTiet = kiemTra.CanhBao.Count > 0
                ? string.Join(" ", kiemTra.CanhBao)
                : "Bộ trọng số không hợp lệ.";
            return Loi(StatusCodes.Status400BadRequest, chiTiet, MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = await _goiY.LuuCauHinhAsync(cauHinh, ct);
        if (ketQua.ThatBaiRoi)
        {
            return Loi(StatusCodes.Status400BadRequest,
                ketQua.Loi ?? "Không lưu được bộ trọng số.",
                ketQua.MaLoi ?? MaLoiChung.DuLieuKhongHopLe);
        }

        _log.LogInformation("Da cap nhat bo trong so AI: phienBan={PhienBan}, userId={UserId}",
            cauHinh.PhienBan, _hienTai.UserId);

        return Ok(ketQua.DuLieu);
    }

    /// <summary>
    /// H4 (bo sung) — Kiem tra bo trong so ma KHONG luu, phuc vu nut "Kiểm tra" tren M13.
    /// </summary>
    /// <remarks>
    /// Luon tra 200 kem hopLe va danh sach canhBao; day la endpoint tham do, khong phai
    /// endpoint luu, nen cau hinh sai van tra 200 chu khong tra 400.
    /// </remarks>
    /// <param name="cauHinh">Bo trong so can kiem tra.</param>
    /// <response code="200">Ket qua kiem tra.</response>
    /// <response code="401">Chua dang nhap.</response>
    /// <response code="403">Chi quan tri duoc dung (§6.2 dong 24).</response>
    [HttpPost("cau-hinh/kiem-tra")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KiemTraCauHinhAiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult KiemTraCauHinh([FromBody] CauHinhAiDto cauHinh)
    {
        var chan = ChanNeuKhongPhaiQuanTri();
        if (chan is not null)
        {
            return chan;
        }

        return Ok(_goiY.KiemTraCauHinh(cauHinh));
    }

    // ------------------------------------------------------------------
    // Ho tro
    // ------------------------------------------------------------------

    /// <summary>
    /// §6.2 dong 24 — chi QUAN_TRI duoc xem nhat ky va tinh chinh trong so AI.
    /// Tra null khi duoc phep di tiep.
    /// </summary>
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
                "Chỉ quản trị hệ thống mới được xem nhật ký và tinh chỉnh trọng số AI.",
                MaLoiChung.KhongCoQuyen);
        }

        return null;
    }

    /// <summary>
    /// Dung mot phan hoi loi chuan (ProblemDetails) kem khoa "loi" va "maLoi" de FE doc
    /// giong het truong Loi / MaLoi cua <see cref="Result"/>.
    /// Thong bao LUON tieng Viet co dau (§ yeu cau chat luong ma nguon).
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
