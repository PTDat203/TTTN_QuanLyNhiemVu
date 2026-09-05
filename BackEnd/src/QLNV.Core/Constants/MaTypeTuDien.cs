namespace QLNV.Core.Constants;

/// <summary>
/// §4 / §5.1 A4 — cot <c>type</c> cua bang DM_TUDIEN.
/// He goc nap nhan trang thai luc chay tu <c>POST /Qtht/api/DmTudien/GetDmTudienByMa</c>
/// (§2 ghi chu, §10.5). App moi PHAI seed du 4 type nay.
/// </summary>
public static class MaTypeTuDien
{
    /// <summary>Truc A — trang thai nhiem vu (§2.1).</summary>
    public const string TrangThaiNv = "TRANGTHAINV";

    /// <summary>Truc B — trang thai phan hoi / kiem tra ket qua (§2.2).</summary>
    public const string TrangThaiPh = "TRANGTHAIPH";

    /// <summary>Loai van ban chi dao (§4.1 truong <c>loaivb</c>).</summary>
    public const string LoaiVb = "LOAIVB";

    /// <summary>Do khan / muc do uu tien (§7.4 muc 4).</summary>
    public const string DoKhan = "DOKHAN";

    public static IReadOnlyList<string> ToanBo() => new[] { TrangThaiNv, TrangThaiPh, LoaiVb, DoKhan };

    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);
}
