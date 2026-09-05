using FluentAssertions;

using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Services;

using QLNV.Tests.HoTro;

using Xunit;

namespace QLNV.Tests;

/// <summary>
/// §1.2 buoc 5 / §5.4 D5 — bo loc combobox "Ket qua xu ly" theo han.
///
/// Bam he goc: tap ung vien 1,2,3,5,6,7,13; con han loai [5,7,8,6]; qua han loai [1,2,4,6,8];
/// ma 13 LUON bi loai. Ket qua: con han =&gt; {1,2,3} · qua han =&gt; {5,7,3}.
/// </summary>
public class TrangThaiHopLeTests
{
    private static readonly DateOnly HomNay = Xuong.HomNay;

    [Fact] // §5.4 D5 — con han: dung tap {1, 2, 3}
    public void ConHan_TraVeDungTapHopLe()
    {
        var ds = HanUtil.TrangThaiHopLeKhiBaoCao(Xuong.HanConHan, HomNay);

        ds.Should().BeEquivalentTo(new[]
        {
            TrangThaiNv.HoanThanh, TrangThaiNv.DangTrienKhai, TrangThaiNv.ChuaTrienKhai
        });
    }

    [Fact] // §5.4 D5 — qua han: dung tap {5, 7, 3}
    public void QuaHan_TraVeDungTapHopLe()
    {
        var ds = HanUtil.TrangThaiHopLeKhiBaoCao(Xuong.HanQuaHan, HomNay);

        ds.Should().BeEquivalentTo(new[]
        {
            TrangThaiNv.HoanThanhSauHan, TrangThaiNv.DangTrienKhaiQuaHan, TrangThaiNv.ChuaTrienKhai
        });
    }

    [Fact] // §5.4 D5 — khong co han thi coi nhu CON HAN
    public void KhongCoHan_CoiNhuConHan()
    {
        var ds = HanUtil.TrangThaiHopLeKhiBaoCao((DateOnly?)null, HomNay);

        ds.Should().BeEquivalentTo(new[]
        {
            TrangThaiNv.HoanThanh, TrangThaiNv.DangTrienKhai, TrangThaiNv.ChuaTrienKhai
        });
    }

    [Fact] // §2.5 — BIEN: so ngay con lai = 0 (han dung hom nay) VAN la CON HAN
    public void HanDungHomNay_VanLaConHan()
    {
        NgayUtil.SoNgayConLai(Xuong.HanDungHomNay, HomNay).Should().Be(0);
        NgayUtil.QuaHan(Xuong.HanDungHomNay, HomNay).Should().BeFalse();

        HanUtil.TrangThaiHopLeKhiBaoCao(Xuong.HanDungHomNay, HomNay).Should().BeEquivalentTo(new[]
        {
            TrangThaiNv.HoanThanh, TrangThaiNv.DangTrienKhai, TrangThaiNv.ChuaTrienKhai
        });
    }

    [Fact] // §2.5 — BIEN: tre dung 1 ngay da la QUA HAN
    public void TreMotNgay_LaQuaHan()
    {
        var han = new DateOnly(2026, 9, 4);

        NgayUtil.SoNgayConLai(han, HomNay).Should().Be(-1);
        NgayUtil.QuaHan(han, HomNay).Should().BeTrue();
        HanUtil.TrangThaiHopLeKhiBaoCao(han, HomNay).Should().Contain(TrangThaiNv.HoanThanhSauHan);
    }

    [Theory] // §1.2 buoc 5 — ma 13 (Gia han) LUON bi loai, ca hai nhanh
    [InlineData(true)]
    [InlineData(false)]
    public void MaGiaHan13_LuonBiLoai(bool quaHan)
    {
        var han = quaHan ? Xuong.HanQuaHan : Xuong.HanConHan;

        HanUtil.TrangThaiHopLeKhiBaoCao(han, HomNay).Should().NotContain(TrangThaiNv.GiaHan);
        HanUtil.TrangThaiBiLoaiKhiBaoCao(han, HomNay).Should().Contain(TrangThaiNv.GiaHan);
    }

    [Theory] // §2.1 — ma 6 (Tu choi) va 97 (Da thu hoi) KHONG bao gio la ket qua tu bao cao
    [InlineData(true)]
    [InlineData(false)]
    public void MaTuChoiVaThuHoi_KhongNamTrongDanhSachBaoCao(bool quaHan)
    {
        var han = quaHan ? Xuong.HanQuaHan : Xuong.HanConHan;
        var ds = HanUtil.TrangThaiHopLeKhiBaoCao(han, HomNay);

        ds.Should().NotContain(TrangThaiNv.TuChoi);
        ds.Should().NotContain(TrangThaiNv.DaThuHoi);
    }

    [Fact] // §5.4 D4 — server validate lai gia tri FE gui len (§6.4: khong tin FE)
    public void HopLeKhiBaoCao_ChanGiaTriNgoaiDanhSach()
    {
        var nv = Xuong.NhiemVu(Guid.NewGuid(), TrangThaiNv.DangTrienKhaiQuaHan, hanXuLyTh: Xuong.HanQuaHan);

        HanUtil.HopLeKhiBaoCao(nv, TrangThaiNv.HoanThanhSauHan, HomNay).Should().BeTrue();
        HanUtil.HopLeKhiBaoCao(nv, TrangThaiNv.HoanThanh, HomNay).Should().BeFalse();
        HanUtil.HopLeKhiBaoCao(nv, TrangThaiNv.GiaHan, HomNay).Should().BeFalse();
        HanUtil.HopLeKhiBaoCao(nv, TrangThaiNv.DaThuHoi, HomNay).Should().BeFalse();
    }

    [Fact] // §2.5 — truc A sau khi bo trang thai bao cao: con han -> 2, qua han -> 7
    public void TrangThaiTheoHan_TraVeHaiGiaTriDuyNhat()
    {
        HanUtil.TrangThaiTheoHan(Xuong.HanConHan, HomNay).Should().Be(TrangThaiNv.DangTrienKhai);
        HanUtil.TrangThaiTheoHan(Xuong.HanDungHomNay, HomNay).Should().Be(TrangThaiNv.DangTrienKhai);
        HanUtil.TrangThaiTheoHan(null, HomNay).Should().Be(TrangThaiNv.DangTrienKhai);
        HanUtil.TrangThaiTheoHan(Xuong.HanQuaHan, HomNay).Should().Be(TrangThaiNv.DangTrienKhaiQuaHan);
    }

    [Fact] // §3.3 M08 — nguong "sap het han" = 3 ngay, tinh ca moc 0
    public void SapHetHan_TinhTheoNguongBaNgay()
    {
        NgayUtil.SapHetHan(new DateOnly(2026, 9, 8), HomNay).Should().BeTrue();   // con 3 ngay
        NgayUtil.SapHetHan(new DateOnly(2026, 9, 5), HomNay).Should().BeTrue();   // con 0 ngay
        NgayUtil.SapHetHan(new DateOnly(2026, 9, 9), HomNay).Should().BeFalse();  // con 4 ngay
        NgayUtil.SapHetHan(new DateOnly(2026, 9, 4), HomNay).Should().BeFalse();  // da qua han
        NgayUtil.SapHetHan(null, HomNay).Should().BeFalse();                      // khong co han
    }

    [Fact] // §2.1 — tap ma truc A khong chua 4, 8, 100 (§7.4 muc 2)
    public void TapMaTrucA_KhongChuaMaDaBo()
    {
        var toanBo = TrangThaiNv.ToanBo();

        toanBo.Should().BeEquivalentTo(new[] { 1, 2, 3, 5, 6, 7, 13, 97 });
        TrangThaiNv.HopLe(4).Should().BeFalse();
        TrangThaiNv.HopLe(8).Should().BeFalse();
        TrangThaiNv.HopLe(100).Should().BeFalse();
    }

    [Fact] // §2.2 — tap ma truc B khong chua 14 (bo nhanh trinh cap tren §1.4)
    public void TapMaTrucB_KhongChuaMa14()
    {
        TrangThaiPh.ToanBo().Should().BeEquivalentTo(new[] { 10, 11, 12 });
        TrangThaiPh.ToanBo().Should().NotContain(14);
    }
}
