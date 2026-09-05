using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QLNV.Api.Auth;
using QLNV.Api.Common;
using QLNV.Core.Common;

namespace QLNV.Api.Authorization;

/// <summary>
/// §6.4 LOP 1 — kiem <c>vaitro</c> duoc phep goi endpoint.
///
/// LOP 2 (quyen theo du lieu + trang thai, bang §6.2) KHONG lam o day: moi controller
/// phai tu kiem bang <c>IQuyenService</c> tren tung ban ghi va tra 403 neu sai.
///
/// §6.4 ghi chu ve route guard: he goc KHONG co <c>canActivate</c> cho bat ky route
/// <c>/admin/...</c> nao — day la LO HONG. O backend, MOI endpoint quan tri BAT BUOC
/// gan <c>[ChiQuanTri]</c>, khong sot cai nao.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class VaiTroAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _choPhep;

    /// <param name="vaiTroChoPhep">
    /// Danh sach vai tro duoc phep (xem <see cref="QLNV.Core.Constants.VaiTro"/>).
    /// De trong = chi yeu cau da dang nhap.
    /// </param>
    public VaiTroAttribute(params string[] vaiTroChoPhep)
    {
        _choPhep = vaiTroChoPhep ?? Array.Empty<string>();
    }

    /// <summary>Danh sach vai tro duoc phep — de kiem thu doc lai.</summary>
    public IReadOnlyList<string> VaiTroChoPhep => _choPhep;

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Ton trong [AllowAnonymous] neu duoc gan cung cho.
        if (context.ActionDescriptor.EndpointMetadata.Any(x => x is IAllowAnonymous))
        {
            return;
        }

        var nguoiDung = context.HttpContext.User;
        var duongDan = context.HttpContext.Request.Path.Value;

        if (nguoiDung?.Identity?.IsAuthenticated != true)
        {
            context.Result = TaoKetQua(StatusCodes.Status401Unauthorized,
                "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.",
                MaLoiChung.ChuaDangNhap, duongDan);
            return;
        }

        if (_choPhep.Length == 0)
        {
            return;
        }

        var vaiTro = nguoiDung.FindFirst(TenClaim.VaiTro)?.Value;
        if (vaiTro is null || !_choPhep.Contains(vaiTro, StringComparer.Ordinal))
        {
            context.Result = TaoKetQua(StatusCodes.Status403Forbidden,
                "Bạn không có quyền thực hiện thao tác này.",
                MaLoiChung.KhongCoQuyen, duongDan);
        }
    }

    private static ObjectResult TaoKetQua(int maTrangThai, string chiTiet, string maLoi, string? duongDan)
    {
        var vanDe = LoiApi.Tao(maTrangThai, chiTiet, maLoi, duongDan);
        return new ObjectResult(vanDe)
        {
            StatusCode = maTrangThai,
            ContentTypes = { LoiApi.LoaiNoiDung }
        };
    }
}

/// <summary>§6.2 dong 21, 23, 24 — chi QUAN_TRI.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ChiQuanTriAttribute : VaiTroAttribute
{
    public ChiQuanTriAttribute() : base(QLNV.Core.Constants.VaiTro.QuanTri)
    {
    }
}

/// <summary>§6.4 lop 1 — nhom ben giao: QUAN_TRI hoac NGUOI_GIAO (§6.2 dong 4-8, 14, 16, 17).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BenGiaoAttribute : VaiTroAttribute
{
    public BenGiaoAttribute()
        : base(QLNV.Core.Constants.VaiTro.QuanTri, QLNV.Core.Constants.VaiTro.NguoiGiao)
    {
    }
}

/// <summary>§6.4 lop 1 — nhom ben lam: chi NGUOI_THUC_HIEN (§6.2 dong 9-13, 15).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BenLamAttribute : VaiTroAttribute
{
    public BenLamAttribute() : base(QLNV.Core.Constants.VaiTro.NguoiThucHien)
    {
    }
}
