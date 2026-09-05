using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>
/// §1.2 buoc 5 / §5.4 D5 — bo loc "Ket qua xu ly" theo han.
/// Ham THUAN TUY: khong phu thuoc DB, khong phu thuoc dong ho he thong.
///
/// Bam dung he goc (<c>thong-tin-xu-ly-nhiem-vu.component.ts:1268-1278</c>):
///  - tap ung vien goc cua combobox: 1, 2, 3, 5, 6, 7, 13;
///  - con han (so ngay con lai &gt;= 0, hoac KHONG co han): loai bo 5, 7, 8, 6;
///  - qua han (&lt; 0):                                     loai bo 1, 2, 4, 6, 8;
///  - ma 13 (Gia han) LUON bi loai khoi combobox nay.
///
/// Ket qua: con han =&gt; {1, 2, 3} · qua han =&gt; {5, 7, 3}.
/// Ma 97 KHONG nam trong tap ung vien: 97 chi do nguoi giao dat khi thu hoi nhiem vu
/// (§2.4 T14), khong phai ket qua nguoi thuc hien tu bao cao.
/// </summary>
public static class HanUtil
{
    /// <summary>Tap ung vien goc truoc khi loc (bam he goc).</summary>
    private static readonly int[] MaUngVienBaoCao =
    {
        TrangThaiNv.HoanThanh,            // 1
        TrangThaiNv.DangTrienKhai,        // 2
        TrangThaiNv.ChuaTrienKhai,        // 3
        TrangThaiNv.HoanThanhSauHan,      // 5
        TrangThaiNv.TuChoi,               // 6
        TrangThaiNv.DangTrienKhaiQuaHan,  // 7
        TrangThaiNv.GiaHan                // 13 — luon bi loai
    };

    /// <summary>Con han: loai bo 5, 7, 8, 6 (ma 8 da bi bo o §2.1 nhung giu day cho dung goc).</summary>
    private static readonly int[] LoaiBoKhiConHan = { 5, 7, 8, 6 };

    /// <summary>Qua han: loai bo 1, 2, 4, 6, 8 (ma 4 va 8 da bi bo o §2.1).</summary>
    private static readonly int[] LoaiBoKhiQuaHan = { 1, 2, 4, 6, 8 };

    /// <summary>Ket qua co dinh khi con han: {1, 2, 3}.</summary>
    private static readonly IReadOnlyList<int> KetQuaConHan = new[]
    {
        TrangThaiNv.HoanThanh, TrangThaiNv.DangTrienKhai, TrangThaiNv.ChuaTrienKhai
    };

    /// <summary>
    /// Ket qua co dinh khi qua han: {5, 7, 3} — dat "Hoan thanh - Sau han" len dau
    /// vi day la lua chon thuong dung nhat khi nhiem vu da tre.
    /// </summary>
    private static readonly IReadOnlyList<int> KetQuaQuaHan = new[]
    {
        TrangThaiNv.HoanThanhSauHan, TrangThaiNv.DangTrienKhaiQuaHan, TrangThaiNv.ChuaTrienKhai
    };

    /// <summary>
    /// §5.4 D5 — danh sach ma trang thai (truc A) nguoi thuc hien duoc chon khi gui bao cao.
    /// </summary>
    /// <param name="nv">Nhiem vu dang bao cao. Chi dung truong <c>hanxulyth</c>.</param>
    /// <param name="homNay">Ngay dung lam moc so sanh han.</param>
    public static IReadOnlyList<int> TrangThaiHopLeKhiBaoCao(DmNhiemVuChiTiet nv, DateOnly homNay)
    {
        ArgumentNullException.ThrowIfNull(nv);
        return TrangThaiHopLeKhiBaoCao(nv.HanXuLyTh, homNay);
    }

    /// <summary>
    /// Bien the nhan thang thoi han — tien cho kiem thu va cho tang goi khong co ca entity.
    /// </summary>
    public static IReadOnlyList<int> TrangThaiHopLeKhiBaoCao(DateOnly? hanXuLyTh, DateOnly homNay) =>
        NgayUtil.QuaHan(hanXuLyTh, homNay) ? KetQuaQuaHan : KetQuaConHan;

    /// <summary>
    /// §5.4 D4 — server VALIDATE LAI gia tri nguoi dung gui len co nam trong danh sach
    /// da loc theo han khong. Khong duoc tin gia tri do FE gui (§6.4).
    /// </summary>
    public static bool HopLeKhiBaoCao(DmNhiemVuChiTiet nv, int trangThai, DateOnly homNay) =>
        TrangThaiHopLeKhiBaoCao(nv, homNay).Contains(trangThai);

    /// <summary>
    /// §2.5 — truc A sau khi bo trang thai bao cao (thu hoi bao cao T8, nghiem thu CHUA_DAT T10):
    /// con han (hoac khong co han) =&gt; 2, qua han =&gt; 7.
    /// </summary>
    public static int TrangThaiTheoHan(DateOnly? hanXuLyTh, DateOnly homNay) =>
        NgayUtil.QuaHan(hanXuLyTh, homNay)
            ? TrangThaiNv.DangTrienKhaiQuaHan
            : TrangThaiNv.DangTrienKhai;

    /// <summary>
    /// Danh sach ma <b>khong hop le</b> tuong ung — tien cho thong bao loi
    /// va cho kiem thu doi khang.
    /// </summary>
    public static IReadOnlyList<int> TrangThaiBiLoaiKhiBaoCao(DateOnly? hanXuLyTh, DateOnly homNay)
    {
        var loaiBo = NgayUtil.QuaHan(hanXuLyTh, homNay) ? LoaiBoKhiQuaHan : LoaiBoKhiConHan;
        var kq = new List<int>();
        foreach (var ma in MaUngVienBaoCao)
        {
            if (ma == TrangThaiNv.GiaHan || loaiBo.Contains(ma)) kq.Add(ma);
        }
        return kq;
    }
}
