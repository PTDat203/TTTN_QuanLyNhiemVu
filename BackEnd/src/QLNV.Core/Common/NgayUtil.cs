using QLNV.Core.Constants;

namespace QLNV.Core.Common;

/// <summary>
/// Tien ich ngay thang thuan tuy — khong phu thuoc DB, khong phu thuoc mui gio he dieu hanh
/// (tru <see cref="HomNay"/>).
///
/// QUY UOC KIEU: <see cref="DateOnly"/> cho NGAY (hanxulyth, hanxulyph, ngaybanhanh,
/// hanxulydexuat), <see cref="DateTime"/> cho MOC THOI GIAN (ngaygiao, ngaytiepnhan,
/// createdate, ngayxuly...). Cach nay tranh han loi "lech 1 ngay" khi so sanh
/// han xu ly voi hom nay o cac mui gio khac nhau.
/// </summary>
public static class NgayUtil
{
    /// <summary>
    /// So ngay con lai toi han. Duong = con han, 0 = het han hom nay, am = qua han.
    /// Tra <c>null</c> khi nhiem vu KHONG co han (<paramref name="han"/> = null).
    /// </summary>
    public static int? SoNgayConLai(DateOnly? han, DateOnly homNay) =>
        han.HasValue ? han.Value.DayNumber - homNay.DayNumber : null;

    /// <summary>
    /// §1.2 buoc 5 / §2.5 — qua han khi so ngay con lai &lt; 0.
    /// KHONG co han thi KHONG bao gio qua han (bam ban flow.js da kiem chung).
    /// </summary>
    public static bool QuaHan(DateOnly? han, DateOnly homNay) =>
        SoNgayConLai(han, homNay) is int d && d < 0;

    /// <summary>
    /// §3.1 M02 / §3.3 M08 — sap het han: con lai tu 0 den
    /// <paramref name="nguong"/> ngay (mac dinh 3), va CHUA qua han.
    /// </summary>
    public static bool SapHetHan(DateOnly? han, DateOnly homNay, int nguong = GioiHan.NguongSapHetHan) =>
        SoNgayConLai(han, homNay) is int d && d >= 0 && d <= nguong;

    /// <summary>Ngay hien tai theo dong ho may chu.</summary>
    public static DateOnly HomNay() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Moc thoi gian hien tai theo dong ho may chu (dung cho createdate/updatedate).</summary>
    public static DateTime BayGio() => DateTime.Now;

    /// <summary>Doi <see cref="DateOnly"/> sang <see cref="DateTime"/> luc 00:00.</summary>
    public static DateTime SangMoc(DateOnly ngay) => ngay.ToDateTime(TimeOnly.MinValue);

    /// <summary>Doi <see cref="DateTime"/> sang <see cref="DateOnly"/> (bo phan gio).</summary>
    public static DateOnly SangNgay(DateTime moc) => DateOnly.FromDateTime(moc);

    /// <summary>Doi <see cref="DateTime"/> co the null sang <see cref="DateOnly"/> co the null.</summary>
    public static DateOnly? SangNgayNullable(DateTime? moc) => moc.HasValue ? DateOnly.FromDateTime(moc.Value) : null;

    /// <summary>Cong them <paramref name="soNgay"/> ngay vao <paramref name="ngay"/>.</summary>
    public static DateOnly CongNgay(DateOnly ngay, int soNgay) => ngay.AddDays(soNgay);

    /// <summary>
    /// Tinh han xu ly tu <c>ngaygiao</c> va <c>songayhxlth</c> (§4.2) khi nguoi giao
    /// chi nhap so ngay thuc hien thay vi chon ngay cu the.
    /// </summary>
    public static DateOnly? TinhHanTuSoNgay(DateOnly ngayGiao, int? soNgay) =>
        soNgay.HasValue && soNgay.Value >= 0 ? ngayGiao.AddDays(soNgay.Value) : null;

    /// <summary>Dinh dang ngay kieu Viet Nam <c>dd/MM/yyyy</c> — dung trong thong bao loi.</summary>
    public static string DinhDang(DateOnly? ngay) =>
        ngay.HasValue ? ngay.Value.ToString("dd/MM/yyyy") : string.Empty;

    /// <summary>Dinh dang moc thoi gian kieu Viet Nam <c>dd/MM/yyyy HH:mm</c>.</summary>
    public static string DinhDangMoc(DateTime? moc) =>
        moc.HasValue ? moc.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty;
}
