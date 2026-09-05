namespace QLNV.Core.Entities;

/// <summary>
/// §4.7 — bang <c>SYS_UNIT</c>. Goc: <c>SysUnitModel</c>.
/// Khoa chinh la <c>unitcode</c> (chuoi), KHONG phai Guid — bam dung he goc va giup
/// truong <c>unitcode</c> o cac bang khac tra cuu truc tiep.
/// </summary>
public class SysUnit
{
    /// <summary>Khoa chinh. Cot <c>unitcode</c>, varchar(50).</summary>
    public string UnitCode { get; set; } = string.Empty;

    /// <summary>Ten don vi. Cot <c>tendonvi</c>.</summary>
    public string TenDonVi { get; set; } = string.Empty;

    /// <summary>Ma don vi cha (self FK toi <see cref="UnitCode"/>). Cot <c>macha</c>.</summary>
    public string? MaCha { get; set; }

    /// <summary>Cap don vi (1 = cao nhat). Cot <c>capdonvi</c>, int.</summary>
    public int CapDonVi { get; set; }

    /// <summary>1 = hoat dong, 0 = ngung. Cot <c>trangthai</c>.</summary>
    public int TrangThai { get; set; } = 1;

    // --- Dieu huong (§4.9: SYS_UNIT.macha -> SYS_UNIT.unitcode, de quy) ---

    /// <summary>Don vi cha.</summary>
    public SysUnit? DonViCha { get; set; }

    /// <summary>Cac don vi con truc tiep.</summary>
    public ICollection<SysUnit> DonViCon { get; set; } = new List<SysUnit>();

    /// <summary>Nguoi dung thuoc don vi nay.</summary>
    public ICollection<SysUser> NguoiDung { get; set; } = new List<SysUser>();
}
