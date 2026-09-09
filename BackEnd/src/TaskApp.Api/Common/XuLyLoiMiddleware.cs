using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskApp.Api.Common;

/// <summary>
/// Middleware bat loi tap trung. Moi ngoai le chua duoc xu ly deu tra ve mot
/// <see cref="ProblemDetails"/> voi thong bao tieng Viet, thay vi trang loi mac dinh
/// cua ASP.NET Core (co the lo chi tiet ky thuat ra ngoai).
/// <para>
/// Dang ky som nhat trong duong ong: <c>app.SuDungXuLyLoi();</c>
/// </para>
/// </summary>
public class XuLyLoiMiddleware
{
    private readonly RequestDelegate _tiepTheo;
    private readonly ILogger<XuLyLoiMiddleware> _nhatKy;
    private readonly IHostEnvironment _moiTruong;

    /// <summary>Khoi tao middleware.</summary>
    /// <param name="tiepTheo">Middleware ke tiep trong duong ong.</param>
    /// <param name="nhatKy">Bo ghi nhat ky.</param>
    /// <param name="moiTruong">Moi truong chay, dung de quyet dinh co lo chi tiet loi hay khong.</param>
    public XuLyLoiMiddleware(
        RequestDelegate tiepTheo,
        ILogger<XuLyLoiMiddleware> nhatKy,
        IHostEnvironment moiTruong)
    {
        _tiepTheo = tiepTheo;
        _nhatKy = nhatKy;
        _moiTruong = moiTruong;
    }

    /// <summary>Diem vao cua middleware.</summary>
    /// <param name="context">Ngu canh HTTP hien tai.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _tiepTheo(context);
        }
        catch (Exception loi)
        {
            await XuLyAsync(context, loi);
        }
    }

    private async Task XuLyAsync(HttpContext context, Exception loi)
    {
        _nhatKy.LogError(
            loi,
            "Loi chua duoc xu ly khi goi {PhuongThuc} {DuongDan}",
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            // Da gui phan dau phan hoi roi thi khong the ghi de, chi con cach ghi nhat ky.
            _nhatKy.LogWarning("Phan hoi da bat dau duoc gui, khong the tra ve ProblemDetails.");
            return;
        }

        var (maHttp, tieuDe, maLoi) = PhanLoai(loi);

        var chiTiet = new ProblemDetails
        {
            Status = maHttp,
            Title = tieuDe,
            Type = "https://tools.ietf.org/html/rfc9110#section-15",
            Instance = context.Request.Path
        };

        chiTiet.Extensions["maLoi"] = maLoi;
        chiTiet.Extensions["maTruyVet"] = context.TraceIdentifier;

        // Chi lo chi tiet ky thuat o moi truong phat trien.
        chiTiet.Detail = _moiTruong.IsDevelopment()
            ? loi.ToString()
            : "Vui lòng liên hệ quản trị hệ thống kèm mã truy vết ở trên.";

        context.Response.Clear();
        context.Response.StatusCode = maHttp;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(chiTiet);
    }

    /// <summary>Quy doi mot ngoai le sang ma HTTP, tieu de tieng Viet va ma loi nghiep vu.</summary>
    private static (int MaHttp, string TieuDe, string MaLoi) PhanLoai(Exception loi)
    {
        return loi switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Bạn không có quyền thực hiện thao tác này.",
                MaLoiChung.KhongCoQuyen),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Không tìm thấy dữ liệu yêu cầu.",
                MaLoiChung.KhongTimThay),

            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Dữ liệu gửi lên không hợp lệ.",
                MaLoiChung.DuLieuKhongHopLe),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Dữ liệu vừa bị người khác thay đổi. Vui lòng tải lại và thử lại.",
                MaLoiChung.LoiNghiepVu),

            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Không ghi được dữ liệu do vi phạm ràng buộc của cơ sở dữ liệu.",
                MaLoiChung.TrungDuLieu),

            OperationCanceledException => (
                499, // Client Closed Request - khong co hang so san trong StatusCodes
                "Yêu cầu đã bị hủy.",
                MaLoiChung.LoiNghiepVu),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Hệ thống gặp lỗi ngoài dự kiến.",
                MaLoiChung.LoiHeThong)
        };
    }
}

/// <summary>Cac ham mo rong dang ky middleware cua ung dung.</summary>
public static class MoRongXuLyLoi
{
    /// <summary>
    /// Dang ky <see cref="XuLyLoiMiddleware"/> vao duong ong xu ly yeu cau.
    /// Nen goi som nhat de bao phu duoc moi middleware phia sau.
    /// </summary>
    /// <param name="app">Bo dung duong ong.</param>
    /// <returns>Chinh <paramref name="app"/> de goi noi tiep.</returns>
    public static IApplicationBuilder SuDungXuLyLoi(this IApplicationBuilder app)
    {
        return app.UseMiddleware<XuLyLoiMiddleware>();
    }
}
