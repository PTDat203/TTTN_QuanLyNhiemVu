namespace QLNV.Core.Constants;

/// <summary>§5.5 E1 — gia tri truong <c>ketQua</c> khi kiem tra ket qua (nghiem thu).</summary>
public static class KetQuaNghiemThu
{
    /// <summary>§2.4 T9 — DAT: trangthaiDvXuly := 11, truc A giu nguyen. DIEM CUOI.</summary>
    public const string Dat = "DAT";

    /// <summary>§2.4 T10 — CHUA DAT: trangthaiDvXuly := 12, truc A ve 2 (con han) / 7 (qua han).</summary>
    public const string ChuaDat = "CHUA_DAT";

    public static IReadOnlyList<string> ToanBo() => new[] { Dat, ChuaDat };

    public static bool HopLe(string? ma) => ma is Dat or ChuaDat;
}

/// <summary>§5.6 F2 — gia tri truong <c>ketQua</c> khi duyet de xuat gia han.</summary>
public static class KetQuaDuyetGiaHan
{
    /// <summary>§2.4 T12 — DUYET: trangthaixulygiahan := 11, solangiahan += 1, cap nhat hanxulyth.</summary>
    public const string Duyet = "DUYET";

    /// <summary>§2.4 T13 — TU_CHOI: trangthaixulygiahan := 12, han giu nguyen.</summary>
    public const string TuChoi = "TU_CHOI";

    public static IReadOnlyList<string> ToanBo() => new[] { Duyet, TuChoi };

    public static bool HopLe(string? ma) => ma is Duyet or TuChoi;
}

/// <summary>
/// §2.4 T4/T5 — gia tri truong <c>ketQua</c> khi nguoi giao xu ly de nghi tu choi.
/// MO RONG: §5 khong co endpoint rieng cho hanh dong nay, nhung §2.4 co hai dong T4/T5.
/// </summary>
public static class KetQuaXuLyTuChoi
{
    /// <summary>§2.4 T4 — chap nhan de nghi tu choi: (6, 10) -&gt; (97, null). DIEM CUOI.</summary>
    public const string ChapNhan = "CHAP_NHAN";

    /// <summary>§2.4 T5 — bac bo de nghi tu choi: (6, 10) -&gt; (2, 12), nguoi thuc hien lam lai.</summary>
    public const string BacBo = "BAC_BO";

    public static IReadOnlyList<string> ToanBo() => new[] { ChapNhan, BacBo };

    public static bool HopLe(string? ma) => ma is ChapNhan or BacBo;
}

/// <summary>§9.5 / §9.6 — truong <c>cheDo</c> cua <c>GoiYResponse</c>.</summary>
public static class CheDoGoiY
{
    /// <summary>He thong da co nhiem vu duoc nghiem thu — cham du 5 dac trung S1..S5.</summary>
    public const string DayDu = "DAY_DU";

    /// <summary>
    /// §9.5 dong cuoi — toan he thong chua co du lieu: chi xep theo S1 va S4
    /// voi trong so 0,6 / 0,4.
    /// </summary>
    public const string KhoiTao = "KHOI_TAO";
}

/// <summary>§5.3 C3 — gia tri tham so <c>vaiTro</c> khi loc danh sach nhiem vu.</summary>
public static class BoLocVaiTro
{
    /// <summary>Nhiem vu do toi giao (userIdGiaoViec = toi hoac useridcreate = toi).</summary>
    public const string ToiGiao = "TOI_GIAO";

    /// <summary>Nhiem vu toi duoc phan cong (co ten trong NHIEMVU_PHANCONG con hieu luc).</summary>
    public const string ToiLam = "TOI_LAM";

    public static IReadOnlyList<string> ToanBo() => new[] { ToiGiao, ToiLam };

    public static bool HopLe(string? ma) => ma is ToiGiao or ToiLam;
}

/// <summary>
/// §9.5 / §9.6 — ma nhan hien thi kem ung vien (truong <c>nhan[]</c>).
/// Ban ai-engine da kiem chung dung dung 5 ma nay.
/// </summary>
public static class MaNhanUngVien
{
    /// <summary>§9.5 dong 1 — chua tung duoc giao nhiem vu nao.</summary>
    public const string NguoiMoi = "NGUOI_MOI";

    /// <summary>§9.4 — he so tin cay &lt; 0,4.</summary>
    public const string DuLieuIt = "DU_LIEU_IT";

    /// <summary>§9.3 dieu 6 — tai trong so &gt;= K x 1,5. Van hien thi, day xuong cuoi.</summary>
    public const string QuaTai = "QUA_TAI";

    /// <summary>§9.4 S5 — dang co nhiem vu qua han.</summary>
    public const string CoQuaHan = "CO_QUA_HAN";

    /// <summary>§9.4 S3(b) — ty le bi tra lai &gt;= 25%.</summary>
    public const string BiTraLai = "BI_TRA_LAI";
}
