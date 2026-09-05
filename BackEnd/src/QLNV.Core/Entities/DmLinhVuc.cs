namespace QLNV.Core.Entities;

/// <summary>
/// Bang <c>DM_LINHVUC</c> — danh muc linh vuc / nghiep vu (§4.1, §4.2, §5.1 A5).
/// Khoa chinh la <c>ma</c> (chuoi) de cot <c>linhvuc</c> o cac bang khac tra cuu truc tiep.
///
/// <c>NhomCha</c> phuc vu §9.4 S1: khi ung vien CHUA tung lam linh vuc L thi xet cac
/// linh vuc CUNG NHOM CHA voi L roi chiet khau 50%.
/// </summary>
public class DmLinhVuc
{
    /// <summary>Ma linh vuc — khoa chinh. Cot <c>ma</c>, varchar(50). Vi du "CNTT_PM".</summary>
    public string Ma { get; set; } = string.Empty;

    /// <summary>Ten hien thi. Cot <c>ten</c>.</summary>
    public string Ten { get; set; } = string.Empty;

    /// <summary>
    /// Ma linh vuc cha (self FK toi <see cref="Ma"/>), null neu la nhom goc.
    /// Cot <c>nhomcha</c>. DAU VAO §9.4 S1 (nhanh chiet khau 50%).
    /// </summary>
    public string? NhomCha { get; set; }

    /// <summary>1 = dang dung, 0 = ngung. Cot <c>trangthai</c>.</summary>
    public int TrangThai { get; set; } = 1;

    /// <summary>Thu tu hien thi trong cay linh vuc (§5.1 A5).</summary>
    public int ThuTu { get; set; }

    // --- Dieu huong ---

    /// <summary>Linh vuc cha.</summary>
    public DmLinhVuc? LinhVucCha { get; set; }

    /// <summary>Cac linh vuc con truc tiep.</summary>
    public ICollection<DmLinhVuc> LinhVucCon { get; set; } = new List<DmLinhVuc>();

    /// <summary>True khi day la linh vuc la (nhiem vu chi gan vao linh vuc la).</summary>
    public bool LaNhomGoc => string.IsNullOrEmpty(NhomCha);
}
