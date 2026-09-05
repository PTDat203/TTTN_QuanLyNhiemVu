namespace QLNV.Core.Abstractions;

/// <summary>
/// Thong tin nguoi dang dang nhap cua request hien tai (§6.4 lop 1).
/// Cai dat o tang API doc tu <c>HttpContext.User</c>; trong kiem thu thi gan cung.
/// </summary>
public interface IHienTai
{
    /// <summary>Id nguoi dang dang nhap. <c>null</c> khi chua dang nhap.</summary>
    Guid? UserId { get; }

    /// <summary>Ten dang nhap. <c>null</c> khi chua dang nhap.</summary>
    string? UserName { get; }

    /// <summary>QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN (§6.1).</summary>
    string? VaiTro { get; }

    /// <summary>Don vi cua nguoi dang dang nhap — dung cho pham vi du lieu (§6.2 dong 20).</summary>
    string? UnitCode { get; }

    /// <summary>True khi request co token hop le.</summary>
    bool DaDangNhap { get; }

    /// <summary>True khi <see cref="VaiTro"/> la QUAN_TRI.</summary>
    bool LaQuanTri { get; }

    /// <summary>True khi <see cref="VaiTro"/> la QUAN_TRI hoac NGUOI_GIAO (§6.4 lop 1).</summary>
    bool LaBenGiao { get; }

    /// <summary>True khi <see cref="VaiTro"/> la NGUOI_THUC_HIEN (§6.4 lop 1).</summary>
    bool LaBenLam { get; }
}
