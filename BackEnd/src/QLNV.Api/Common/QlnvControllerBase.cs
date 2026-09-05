using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using MaVaiTro = QLNV.Core.Constants.VaiTro;

namespace QLNV.Api.Common;

/// <summary>
/// Lop co so cho MOI controller nghiep vu.
///
/// §6.4 — kiem quyen HAI LOP tren tung endpoint:
///   LOP 1: <c>vaitro</c> co duoc phep goi endpoint nay khong (cac o ❌ cua bang §6.2)
///          — dung <see cref="BatBuocBenGiao"/> / <see cref="BatBuocBenLam"/>.
///   LOP 2: quyen theo DU LIEU + TRANG THAI — goi <see cref="IQuyenService"/> roi doc dung
///          co tuong ung; sai thi tra 403 (<see cref="Loi403"/>).
/// TUYET DOI khong tin bat ky co nao do FE gui len.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class QlnvControllerBase : ControllerBase
{
    protected QlnvControllerBase(IHienTai hienTai)
    {
        HienTai = hienTai;
    }

    /// <summary>Nguoi dang dang nhap (doc tu JWT).</summary>
    protected IHienTai HienTai { get; }

    /// <summary>Id nguoi dang dang nhap; <see cref="Guid.Empty"/> khi chua dang nhap.</summary>
    protected Guid UserId => HienTai.UserId ?? Guid.Empty;

    /// <summary>Moc "hom nay" dung cho moi phep so han trong request nay (§2.5).</summary>
    protected DateOnly HomNay => NgayUtil.HomNay();

    // ---------------------------------------------------------------
    // Cac ket qua loi chuan
    // ---------------------------------------------------------------

    protected IActionResult Loi400(string thongBao, string? ma = MaLoiChung.DuLieuKhongHopLe) =>
        BadRequest(new LoiApiDto(thongBao, ma));

    protected IActionResult Loi401(string thongBao = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.") =>
        StatusCode(StatusCodes.Status401Unauthorized, new LoiApiDto(thongBao, MaLoiChung.ChuaDangNhap));

    /// <summary>§6.4 — sai quyen theo vai tro hoac theo du lieu/trang thai deu tra 403.</summary>
    protected IActionResult Loi403(string thongBao) =>
        StatusCode(StatusCodes.Status403Forbidden, new LoiApiDto(thongBao, MaLoiChung.KhongCoQuyen));

    protected IActionResult Loi404(string thongBao) =>
        NotFound(new LoiApiDto(thongBao, MaLoiChung.KhongTimThay));

    protected IActionResult Loi409(string thongBao, string? ma = MaLoiChung.SaiTrangThai) =>
        Conflict(new LoiApiDto(thongBao, ma));

    /// <summary>Anh xa <see cref="MaLoiChung"/> sang ma HTTP (§6.4).</summary>
    protected IActionResult TuLoi(string? thongBao, string? maLoi)
    {
        var tb = string.IsNullOrWhiteSpace(thongBao) ? "Thao tác không thực hiện được." : thongBao;
        return maLoi switch
        {
            MaLoiChung.KhongTimThay => Loi404(tb),
            MaLoiChung.KhongCoQuyen => Loi403(tb),
            MaLoiChung.ChuaDangNhap => Loi401(tb),
            MaLoiChung.SaiTrangThai => Loi409(tb, MaLoiChung.SaiTrangThai),
            MaLoiChung.ViPhamRangBuoc => Loi409(tb, MaLoiChung.ViPhamRangBuoc),
            MaLoiChung.TrungNoiDung => Loi409(tb, MaLoiChung.TrungNoiDung),
            _ => Loi400(tb, maLoi ?? MaLoiChung.DuLieuKhongHopLe)
        };
    }

    /// <summary>Chuyen <see cref="Result"/> that bai sang IActionResult; null khi thanh cong.</summary>
    protected IActionResult? NeuLoi(Result kq) => kq.ThanhCong ? null : TuLoi(kq.Loi, kq.MaLoi);

    /// <summary>Chuyen <see cref="Result{T}"/> that bai sang IActionResult; null khi thanh cong.</summary>
    protected IActionResult? NeuLoi<T>(Result<T> kq) => kq.ThanhCong ? null : TuLoi(kq.Loi, kq.MaLoi);

    // ---------------------------------------------------------------
    // §6.4 LOP 1 — kiem vai tro
    // ---------------------------------------------------------------

    /// <summary>
    /// LOP 1 cho cac hanh dong cua BEN GIAO (§6.2 dong 2,3,4,5,6,7,8,14,16,17):
    /// chi QUAN_TRI hoac NGUOI_GIAO. Tra ve IActionResult loi, hoac null neu dat.
    /// </summary>
    protected IActionResult? BatBuocBenGiao(string tenHanhDong)
    {
        if (!HienTai.DaDangNhap || UserId == Guid.Empty) return Loi401();
        if (!MaVaiTro.LaBenGiao(HienTai.VaiTro))
        {
            return Loi403($"Vai trò của bạn không được phép {tenHanhDong}. " +
                          "Chức năng này chỉ dành cho người giao việc hoặc quản trị.");
        }
        return null;
    }

    /// <summary>
    /// LOP 1 cho cac hanh dong cua BEN LAM (§6.2 dong 9,10,11,12,13,15):
    /// chi NGUOI_THUC_HIEN. Quan tri va nguoi giao KHONG duoc lam thay (cot ❌ cua §6.2).
    /// </summary>
    protected IActionResult? BatBuocBenLam(string tenHanhDong)
    {
        if (!HienTai.DaDangNhap || UserId == Guid.Empty) return Loi401();
        if (!MaVaiTro.LaBenLam(HienTai.VaiTro))
        {
            return Loi403($"Vai trò của bạn không được phép {tenHanhDong}. " +
                          "Chức năng này chỉ dành cho người thực hiện nhiệm vụ.");
        }
        return null;
    }

    /// <summary>LOP 1 toi thieu: chi can dang nhap (§6.2 dong 18, 19 — moi vai deu co the co quyen).</summary>
    protected IActionResult? BatBuocDangNhap()
    {
        if (!HienTai.DaDangNhap || UserId == Guid.Empty) return Loi401();
        return null;
    }

    // ---------------------------------------------------------------
    // Kiem tra du lieu dau vao (FluentValidation)
    // ---------------------------------------------------------------

    /// <summary>
    /// Chay validator cua <typeparamref name="T"/> neu co dang ky trong DI.
    /// Tra ve 400 kem danh sach loi tieng Viet; null khi hop le.
    /// </summary>
    protected async Task<IActionResult?> KiemTraAsync<T>(T? model, CancellationToken ct) where T : class
    {
        if (model is null) return Loi400("Thiếu dữ liệu gửi lên.");

        var validator = HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null) return null; // chua dang ky validator => bo qua, cac guard nghiep vu van chay

        var kq = await validator.ValidateAsync(model, ct);
        if (kq.IsValid) return null;

        var goi = new LoiApiDto("Dữ liệu gửi lên không hợp lệ.", MaLoiChung.DuLieuKhongHopLe);
        foreach (var e in kq.Errors) goi.ChiTiet.Add(e.ErrorMessage);
        return BadRequest(goi);
    }

    /// <summary>Chuan hoa so trang / kich thuoc trang ve khoang cho phep.</summary>
    protected static (int Trang, int KichThuoc) ChuanHoaTrang(int trang, int kichThuoc)
    {
        var t = trang < 1 ? 1 : trang;
        var k = kichThuoc < 1 ? QLNV.Core.Constants.GioiHan.KichThuocTrangMacDinh : kichThuoc;
        if (k > QLNV.Core.Constants.GioiHan.KichThuocTrangToiDa) k = QLNV.Core.Constants.GioiHan.KichThuocTrangToiDa;
        return (t, k);
    }
}
