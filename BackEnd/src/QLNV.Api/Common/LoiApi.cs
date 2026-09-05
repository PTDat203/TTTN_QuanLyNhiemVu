using Microsoft.AspNetCore.Mvc;
using QLNV.Core.Common;

namespace QLNV.Api.Common;

/// <summary>
/// Noi DUY NHAT sinh <see cref="ProblemDetails"/> cua toan bo API.
/// Moi thong bao tra ve nguoi dung deu la TIENG VIET CO DAU (yeu cau chat luong ma nguon).
/// §6.4: sai quyen theo du lieu tra 403, khong tra 404 de tranh lo thong tin sai lech.
/// </summary>
public static class LoiApi
{
    /// <summary>Kieu noi dung chuan cua ProblemDetails (RFC 7807).</summary>
    public const string LoaiNoiDung = "application/problem+json";

    /// <summary>Ten khoa mo rong chua ma loi nghiep vu (<see cref="MaLoiChung"/>).</summary>
    public const string KhoaMaLoi = "maLoi";

    /// <summary>Ten khoa mo rong chua ma vet cua request.</summary>
    public const string KhoaMaVet = "maVet";

    /// <summary>
    /// Ten khoa mo rong lap lai thong bao tieng Viet.
    /// LY DO: cac controller nghiep vu (VanBan / NhiemVu / GiaHan / NghiemThu / Ai /
    /// Dashboard / Jobs) tra loi bang <c>QLNV.Api.Common.LoiApiDto</c> voi hai khoa
    /// <c>thongBao</c> + <c>maLoi</c>. Them <c>thongBao</c> vao ProblemDetails de FE
    /// chi can MOT ham doc loi duy nhat cho ca hai dang than phan hoi.
    /// </summary>
    public const string KhoaThongBao = "thongBao";

    /// <summary>Tieu de tieng Viet theo ma trang thai HTTP.</summary>
    public static string TieuDe(int maTrangThai) => maTrangThai switch
    {
        400 => "Dữ liệu không hợp lệ",
        401 => "Chưa đăng nhập",
        403 => "Không có quyền thực hiện",
        404 => "Không tìm thấy",
        405 => "Phương thức không được hỗ trợ",
        409 => "Xung đột trạng thái",
        413 => "Tệp vượt quá dung lượng cho phép",
        415 => "Định dạng dữ liệu không được hỗ trợ",
        422 => "Dữ liệu không xử lý được",
        500 => "Lỗi hệ thống",
        _ => "Yêu cầu không thực hiện được"
    };

    /// <summary>Mo ta mac dinh tieng Viet theo ma trang thai HTTP.</summary>
    public static string MoTaMacDinh(int maTrangThai) => maTrangThai switch
    {
        400 => "Dữ liệu gửi lên không hợp lệ.",
        401 => "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.",
        403 => "Bạn không có quyền thực hiện thao tác này.",
        404 => "Không tìm thấy dữ liệu yêu cầu.",
        405 => "Phương thức HTTP không được hỗ trợ cho đường dẫn này.",
        409 => "Thao tác không hợp lệ ở trạng thái hiện tại của dữ liệu.",
        500 => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại hoặc liên hệ quản trị hệ thống.",
        _ => "Yêu cầu không thực hiện được."
    };

    /// <summary>
    /// Anh xa <see cref="MaLoiChung"/> sang ma trang thai HTTP (§6.4).
    /// </summary>
    public static int MaTrangThaiTheoMaLoi(string? maLoi) => maLoi switch
    {
        MaLoiChung.ChuaDangNhap => StatusCodes.Status401Unauthorized,
        MaLoiChung.KhongCoQuyen => StatusCodes.Status403Forbidden,
        MaLoiChung.KhongTimThay => StatusCodes.Status404NotFound,
        MaLoiChung.SaiTrangThai => StatusCodes.Status409Conflict,
        MaLoiChung.ViPhamRangBuoc => StatusCodes.Status409Conflict,
        MaLoiChung.TrungNoiDung => StatusCodes.Status409Conflict,
        MaLoiChung.LoiTep => StatusCodes.Status400BadRequest,
        MaLoiChung.DuLieuKhongHopLe => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };

    /// <summary>Dung mot <see cref="ProblemDetails"/> hoan chinh.</summary>
    /// <param name="maTrangThai">Ma trang thai HTTP.</param>
    /// <param name="chiTiet">Thong bao tieng Viet co dau cho nguoi dung; null thi dung mo ta mac dinh.</param>
    /// <param name="maLoi">Ma loi nghiep vu (<see cref="MaLoiChung"/>), tuy chon.</param>
    /// <param name="duongDan">Duong dan cua request, dua vao truong <c>instance</c>.</param>
    /// <param name="maVet">Ma vet de doi chieu log, tuy chon.</param>
    public static ProblemDetails Tao(
        int maTrangThai,
        string? chiTiet = null,
        string? maLoi = null,
        string? duongDan = null,
        string? maVet = null)
    {
        var chiTiet2 = string.IsNullOrWhiteSpace(chiTiet) ? MoTaMacDinh(maTrangThai) : chiTiet;

        var vanDe = new ProblemDetails
        {
            Status = maTrangThai,
            Title = TieuDe(maTrangThai),
            Detail = chiTiet2,
            Instance = duongDan
        };

        // Lap lai thong bao duoi khoa "thongBao" cho dong nhat voi LoiApiDto.
        vanDe.Extensions[KhoaThongBao] = chiTiet2;

        if (!string.IsNullOrWhiteSpace(maLoi))
        {
            vanDe.Extensions[KhoaMaLoi] = maLoi;
        }

        if (!string.IsNullOrWhiteSpace(maVet))
        {
            vanDe.Extensions[KhoaMaVet] = maVet;
        }

        return vanDe;
    }

    /// <summary>Ghi <see cref="ProblemDetails"/> ra response duoi dang JSON.</summary>
    public static async Task GhiRaAsync(HttpContext ctx, ProblemDetails vanDe, CancellationToken ct = default)
    {
        ctx.Response.StatusCode = vanDe.Status ?? StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = LoaiNoiDung;
        await ctx.Response.WriteAsJsonAsync(vanDe, options: null, contentType: LoaiNoiDung, cancellationToken: ct);
    }
}
