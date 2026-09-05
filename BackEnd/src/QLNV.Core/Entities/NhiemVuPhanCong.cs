namespace QLNV.Core.Entities;

/// <summary>
/// §4.3 — bang <c>NHIEMVU_PHANCONG</c> (bang noi nguoi duoc giao). Goc: <c>UserThPhModel</c>.
///
/// Rang buoc §4.3:
///  - moi <c>idnvchitiet</c> phai co it nhat 1 ban ghi <c>vaitro = 'CHUTRI'</c> con hieu luc;
///  - mot <c>userid</c> khong duoc vua CHUTRI vua PHOIHOP tren cung 1 nhiem vu.
/// </summary>
public class NhiemVuPhanCong
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK toi <see cref="DmNhiemVuChiTiet"/>. BAT BUOC. Cot <c>idnvchitiet</c>.</summary>
    public Guid IdNvChiTiet { get; set; }

    /// <summary>FK toi <see cref="SysUser"/>. BAT BUOC. Cot <c>userid</c>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Don vi cua nguoi duoc phan cong. BAT BUOC. Cot <c>unitcode</c>, varchar(50).</summary>
    public string UnitCode { get; set; } = string.Empty;

    /// <summary>
    /// CHUTRI hoac PHOIHOP. BAT BUOC. Cot <c>vaitro</c>, varchar(10).
    /// Cai tien so voi goc (goc phan biet bang mang listUserTh / listUserPh).
    /// </summary>
    public string VaiTro { get; set; } = Constants.VaiTroPhanCong.ChuTri;

    /// <summary>Ai tao phan cong nay. BAT BUOC. Cot <c>useridCreate</c>.</summary>
    public Guid UserIdCreate { get; set; }

    /// <summary>
    /// 1 = con hieu luc, 0 = da thu hoi phan cong (§5.3 C7). BAT BUOC. Cot <c>trangthai</c>.
    /// </summary>
    public int TrangThai { get; set; } = Constants.TrangThaiPhanCong.ConHieuLuc;

    /// <summary>Thoi diem tao. BAT BUOC. Cot <c>createdate</c>.</summary>
    public DateTime CreateDate { get; set; }

    // --- Dieu huong ---

    /// <summary>Nhiem vu tuong ung.</summary>
    public DmNhiemVuChiTiet? NhiemVu { get; set; }

    /// <summary>Nguoi duoc phan cong.</summary>
    public SysUser? NguoiDung { get; set; }

    /// <summary>Tien ich: ban ghi con hieu luc va giu vai CHU TRI.</summary>
    public bool LaChuTriConHieuLuc =>
        TrangThai == Constants.TrangThaiPhanCong.ConHieuLuc
        && VaiTro == Constants.VaiTroPhanCong.ChuTri;

    /// <summary>Tien ich: ban ghi con hieu luc va giu vai PHOI HOP.</summary>
    public bool LaPhoiHopConHieuLuc =>
        TrangThai == Constants.TrangThaiPhanCong.ConHieuLuc
        && VaiTro == Constants.VaiTroPhanCong.PhoiHop;
}
