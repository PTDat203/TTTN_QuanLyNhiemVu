using QLNV.Api.Common;
using QLNV.Core.Common;

namespace QLNV.Api.Middleware;

/// <summary>
/// Middleware bat loi TAP TRUNG cua toan bo API.
///
/// Hai nhiem vu:
/// 1. Bat moi exception chua duoc xu ly -> tra 500 kem <c>ProblemDetails</c> tieng Viet,
///    ghi log day du (KHONG nuot exception, KHONG lo stack trace cho nguoi dung o moi truong that).
/// 2. Bo sung than phan hoi cho 401/403 do tang xac thuc/phan quyen sinh ra (§6.4) —
///    mac dinh ASP.NET Core tra 401/403 voi than RONG, FE khong hien duoc thong bao.
/// </summary>
public sealed class XuLyLoiMiddleware
{
    private readonly RequestDelegate _tiepTheo;
    private readonly ILogger<XuLyLoiMiddleware> _nhatKy;
    private readonly IHostEnvironment _moiTruong;

    public XuLyLoiMiddleware(
        RequestDelegate tiepTheo,
        ILogger<XuLyLoiMiddleware> nhatKy,
        IHostEnvironment moiTruong)
    {
        _tiepTheo = tiepTheo ?? throw new ArgumentNullException(nameof(tiepTheo));
        _nhatKy = nhatKy ?? throw new ArgumentNullException(nameof(nhatKy));
        _moiTruong = moiTruong ?? throw new ArgumentNullException(nameof(moiTruong));
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        try
        {
            await _tiepTheo(ctx);

            // 401/403 do UseAuthentication/UseAuthorization sinh ra co than rong.
            if (!ctx.Response.HasStarted
                && ctx.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden
                && ctx.Response.ContentLength is null or 0)
            {
                var ma = ctx.Response.StatusCode == StatusCodes.Status401Unauthorized
                    ? MaLoiChung.ChuaDangNhap
                    : MaLoiChung.KhongCoQuyen;

                var vanDeQuyen = LoiApi.Tao(
                    ctx.Response.StatusCode,
                    chiTiet: null,
                    maLoi: ma,
                    duongDan: ctx.Request.Path.Value,
                    maVet: ctx.TraceIdentifier);

                await LoiApi.GhiRaAsync(ctx, vanDeQuyen, ctx.RequestAborted);
            }
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // Nguoi dung dong tab / huy request: khong phai loi he thong.
            _nhatKy.LogInformation("Yêu cầu {Duong} bị huỷ bởi phía gọi.", ctx.Request.Path.Value);
        }
        catch (Exception loi)
        {
            _nhatKy.LogError(loi, "Lỗi không bắt được khi xử lý {Phuong} {Duong}",
                ctx.Request.Method, ctx.Request.Path.Value);

            if (ctx.Response.HasStarted)
            {
                // Da gui header roi thi khong the ghi de -> nem lai cho tang duoi ket thuc ket noi.
                throw;
            }

            // HttpResponse khong co Clear(); chi can bo ContentLength cu, LoiApi se dat lai
            // StatusCode va ContentType. KHONG xoa Headers de giu nguyen header CORS.
            ctx.Response.ContentLength = null;

            var chiTiet = _moiTruong.IsDevelopment()
                ? $"Đã xảy ra lỗi không mong muốn: {loi.Message}"
                : null;

            var vanDe = LoiApi.Tao(
                StatusCodes.Status500InternalServerError,
                chiTiet,
                maLoi: null,
                duongDan: ctx.Request.Path.Value,
                maVet: ctx.TraceIdentifier);

            if (_moiTruong.IsDevelopment())
            {
                vanDe.Extensions["kieuLoi"] = loi.GetType().FullName;
            }

            await LoiApi.GhiRaAsync(ctx, vanDe, CancellationToken.None);
        }
    }
}

/// <summary>Ham mo rong dang ky <see cref="XuLyLoiMiddleware"/>.</summary>
public static class XuLyLoiMiddlewareExtensions
{
    /// <summary>Cam <see cref="XuLyLoiMiddleware"/> vao dau duong ong xu ly.</summary>
    public static IApplicationBuilder UseXuLyLoiTapTrung(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<XuLyLoiMiddleware>();
    }
}
