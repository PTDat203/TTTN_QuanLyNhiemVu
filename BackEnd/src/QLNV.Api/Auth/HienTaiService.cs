using System.Security.Claims;
using QLNV.Core.Abstractions;

namespace QLNV.Api.Auth;

/// <summary>
/// §6.4 lop 1 — cai dat <see cref="IHienTai"/> bang cach doc claim tu
/// <c>HttpContext.User</c>. KHONG bao gio doc vai tro tu body/header do FE gui len.
/// </summary>
public sealed class HienTaiService : IHienTai
{
    private readonly IHttpContextAccessor _truyCap;

    public HienTaiService(IHttpContextAccessor truyCap)
    {
        _truyCap = truyCap ?? throw new ArgumentNullException(nameof(truyCap));
    }

    private ClaimsPrincipal? ChuThe => _truyCap.HttpContext?.User;

    private string? DocClaim(string ten) => ChuThe?.FindFirst(ten)?.Value;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var chuoi = DocClaim(TenClaim.UserId) ?? DocClaim(ClaimTypes.NameIdentifier);
            return Guid.TryParse(chuoi, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? UserName => DocClaim(TenClaim.UserName);

    /// <inheritdoc />
    public string? VaiTro => DocClaim(TenClaim.VaiTro);

    /// <inheritdoc />
    public string? UnitCode => DocClaim(TenClaim.UnitCode);

    /// <inheritdoc />
    public bool DaDangNhap => ChuThe?.Identity?.IsAuthenticated == true && UserId is not null;

    /// <inheritdoc />
    public bool LaQuanTri => VaiTro == QLNV.Core.Constants.VaiTro.QuanTri;

    /// <inheritdoc />
    public bool LaBenGiao => QLNV.Core.Constants.VaiTro.LaBenGiao(VaiTro);

    /// <inheritdoc />
    public bool LaBenLam => QLNV.Core.Constants.VaiTro.LaBenLam(VaiTro);
}
