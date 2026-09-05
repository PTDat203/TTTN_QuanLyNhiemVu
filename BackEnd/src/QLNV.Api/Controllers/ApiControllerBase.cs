using Microsoft.AspNetCore.Mvc;
using QLNV.Api.Common;
using QLNV.Core.Common;

namespace QLNV.Api.Controllers;

/// <summary>
/// Lop nen cua moi controller: prefix <c>/api/v1</c> (§5) + ham chuyen
/// <see cref="Result"/> / <see cref="Result{T}"/> sang <see cref="IActionResult"/>.
///
/// QUY UOC: tang nghiep vu KHONG nem exception cho loi nghiep vu, chi tra Result;
/// controller anh xa <see cref="MaLoiChung"/> sang ma HTTP dung §6.4.
/// </summary>
// LUU Y: KHONG dat [Route] o lop nen. RouteAttribute co AllowMultiple = true va
// Inherited = true, neu dat o day thi moi controller con se sinh THEM mot mau duong
// dan thua ("api/v1/..."), gay trung tuyen. Moi controller tu khai duong dan day du.
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Tao phan hoi loi tieng Viet theo ma trang thai HTTP.</summary>
    protected IActionResult Loi(int maTrangThai, string chiTiet, string? maLoi = null)
    {
        var vanDe = LoiApi.Tao(maTrangThai, chiTiet, maLoi, HttpContext?.Request.Path.Value,
            HttpContext?.TraceIdentifier);
        return new ObjectResult(vanDe)
        {
            StatusCode = maTrangThai,
            ContentTypes = { LoiApi.LoaiNoiDung }
        };
    }

    /// <summary>400 — du lieu dau vao khong hop le.</summary>
    protected IActionResult LoiDuLieu(string chiTiet) =>
        Loi(StatusCodes.Status400BadRequest, chiTiet, MaLoiChung.DuLieuKhongHopLe);

    /// <summary>403 — sai quyen theo du lieu / trang thai (§6.4 lop 2).</summary>
    protected IActionResult LoiKhongCoQuyen(string? chiTiet = null) =>
        Loi(StatusCodes.Status403Forbidden,
            chiTiet ?? "Bạn không có quyền thực hiện thao tác này.",
            MaLoiChung.KhongCoQuyen);

    /// <summary>404 — khong tim thay ban ghi.</summary>
    protected IActionResult LoiKhongTimThay(string? chiTiet = null) =>
        Loi(StatusCodes.Status404NotFound,
            chiTiet ?? "Không tìm thấy dữ liệu yêu cầu.",
            MaLoiChung.KhongTimThay);

    /// <summary>Chuyen <see cref="Result"/> that bai sang phan hoi HTTP; thanh cong tra 204.</summary>
    protected IActionResult TuKetQua(Result ketQua)
    {
        ArgumentNullException.ThrowIfNull(ketQua);

        if (ketQua.ThanhCong)
        {
            return NoContent();
        }

        return Loi(LoiApi.MaTrangThaiTheoMaLoi(ketQua.MaLoi),
            ketQua.Loi ?? "Yêu cầu không thực hiện được.",
            ketQua.MaLoi);
    }

    /// <summary>Chuyen <see cref="Result{T}"/> sang phan hoi HTTP; thanh cong tra 200 kem du lieu.</summary>
    protected IActionResult TuKetQua<T>(Result<T> ketQua)
    {
        ArgumentNullException.ThrowIfNull(ketQua);

        if (ketQua.ThanhCong)
        {
            return Ok(ketQua.DuLieu);
        }

        return Loi(LoiApi.MaTrangThaiTheoMaLoi(ketQua.MaLoi),
            ketQua.Loi ?? "Yêu cầu không thực hiện được.",
            ketQua.MaLoi);
    }
}
