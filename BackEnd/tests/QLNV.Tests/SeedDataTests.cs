using FluentAssertions;

using QLNV.Core.Constants;
using QLNV.Core.Entities;

using QLNV.Tests.HoTro;

using Xunit;

namespace QLNV.Tests;

/// <summary>
/// §8 T4 / T9 — bo du lieu mau la TIEN DE cua AI: "&gt;= 300 nhiem vu lich su da ket thuc,
/// phan bo theo 5-8 linh vuc va 15-20 nguoi dung, co dung han / tre han / bi tra lai".
/// §8.1 moc M4 — "Precision@3 &gt;= 0,6 tren tap mo phong".
///
/// Chay <c>DbInitializer</c> tren CSDL trong bo nho roi kiem tinh nhat quan cua du lieu.
/// </summary>
public class SeedDataTests
{
    [Fact] // §8 T9 — khoi luong du lieu lich su
    public async Task Seed_CoItNhat320NhiemVu()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhiemVuAsync();

        ds.Should().HaveCountGreaterThanOrEqualTo(320,
            "§8 T9 yeu cau >= 300 nhiem vu lich su; bo mau chot o 320 de AI co du du lieu");
    }

    [Fact] // §8 T9 — 5-8 linh vuc, 15-20 nguoi dung
    public async Task Seed_CoDuLinhVucVaNguoiDung()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var linhVuc = await mt.DocAsync<DmLinhVuc>();
        var nguoiDung = await mt.NguoiDungAsync();

        linhVuc.Where(l => !l.LaNhomGoc).Should().HaveCountGreaterThanOrEqualTo(8,
            "§8 T9: phan bo theo 5-8 linh vuc");
        nguoiDung.Should().HaveCountGreaterThanOrEqualTo(15, "§8 T9: 15-20 nguoi dung");
        nguoiDung.Count(u => u.VaiTro == VaiTro.NguoiThucHien && u.TrangThai == 1)
            .Should().BeGreaterThanOrEqualTo(10, "§9.3 loc cung phai con du ung vien de cham diem");
    }

    [Fact] // §7.4 muc 2 — chi duoc dung ma trang thai 1/2/3/5/6/7/13/97
    public async Task Seed_KhongCoTrangThaiNgoaiTapHopLe()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhiemVuAsync();

        ds.Select(n => n.TrangThai).Distinct().Should().BeSubsetOf(new[] { 1, 2, 3, 5, 6, 7, 13, 97 });
        ds.Select(n => n.TrangThaiDvXuly).Distinct().Should().BeSubsetOf(new int?[] { null, 10, 11, 12 });
        ds.Select(n => n.TrangThaiXuLyGiaHan).Distinct().Should().BeSubsetOf(new int?[] { null, 10, 11, 12 });
    }

    [Fact] // §2.3 — solangiahan toi da 2
    public async Task Seed_SoLanGiaHanKhongVuotNguong()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhiemVuAsync();

        ds.Should().OnlyContain(n => n.SoLanGiaHan >= 0 && n.SoLanGiaHan <= GioiHan.SoLanGiaHanToiDa);
    }

    [Fact] // §4.3 / §1.2 buoc 2 — moi nhiem vu phai co it nhat MOT nguoi CHUTRI
    public async Task Seed_MoiNhiemVuCoItNhatMotChuTri()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var dsNv = await mt.NhiemVuAsync();
        var dsPc = await mt.PhanCongAsync();
        var chuTriTheoNv = dsPc
            .Where(p => p.TrangThai == TrangThaiPhanCong.ConHieuLuc && p.VaiTro == VaiTroPhanCong.ChuTri)
            .Select(p => p.IdNvChiTiet)
            .ToHashSet();

        var thieu = dsNv.Where(n => !chuTriTheoNv.Contains(n.Id)).Select(n => n.Id).ToList();

        thieu.Should().BeEmpty("moi nhiem vu deu phai co chu tri (§4.3)");
    }

    [Fact] // §6.3 / §8 T6 — rang buoc loai tru: cung mot nguoi khong duoc vua CHUTRI vua PHOIHOP
    public async Task Seed_KhongAiVuaChuTriVuaPhoiHopTrenCungNhiemVu()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var dsPc = await mt.PhanCongAsync();
        var viPham = dsPc
            .Where(p => p.TrangThai == TrangThaiPhanCong.ConHieuLuc)
            .GroupBy(p => new { p.IdNvChiTiet, p.UserId })
            .Where(g => g.Select(x => x.VaiTro).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        viPham.Should().BeEmpty("§8 T6 — rang buoc loai tru chu tri ⇄ phoi hop theo tung dong");
    }

    [Fact] // §4.3 — moi ban ghi phan cong phai tro toi nguoi dung va nhiem vu co that
    public async Task Seed_PhanCongThamChieuHopLe()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var idNv = (await mt.NhiemVuAsync()).Select(n => n.Id).ToHashSet();
        var idUser = (await mt.NguoiDungAsync()).Select(u => u.Id).ToHashSet();
        var dsPc = await mt.PhanCongAsync();

        dsPc.Should().OnlyContain(p => idNv.Contains(p.IdNvChiTiet));
        dsPc.Should().OnlyContain(p => idUser.Contains(p.UserId));
        dsPc.Select(p => p.VaiTro).Distinct().Should()
            .BeSubsetOf(new[] { VaiTroPhanCong.ChuTri, VaiTroPhanCong.PhoiHop });
    }

    [Fact] // §2.6 — bo mau phai co du ca hai diem cuoi va ca nhanh dang mo
    public async Task Seed_CoDuCacTinhHuongCuaMayTrangThai()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhiemVuAsync();

        ds.Count(n => TrangThaiNv.LaDiemCuoi(n.TrangThai, n.TrangThaiDvXuly) == DiemCuoi.HoanThanhNghiemThu)
            .Should().BeGreaterThanOrEqualTo(300, "§9.4 chi dem viec DA NGHIEM THU");
        ds.Should().Contain(n => TrangThaiNv.DangMo.Contains(n.TrangThai), "phai con viec dang mo cho §9.4 S4");
        ds.Should().Contain(n => n.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan, "phai co viec qua han cho §9.4 S5");
    }

    [Fact] // §9.4 S3 (a) — phai co CA hoan thanh dung han (1) lan sau han (5)
    public async Task Seed_CoCaHoanThanhDungHanVaSauHan()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhiemVuAsync();

        ds.Should().Contain(n => n.TrangThai == TrangThaiNv.HoanThanh
                                 && n.TrangThaiDvXuly == TrangThaiPh.DaXacNhan);
        ds.Should().Contain(n => n.TrangThai == TrangThaiNv.HoanThanhSauHan
                                 && n.TrangThaiDvXuly == TrangThaiPh.DaXacNhan);
    }

    [Fact] // §4.4 — lich su xu ly chi dung cac ma loai da khai (§4.4 / LoaiXuLy)
    public async Task Seed_LichSuXuLyDungMaLoaiHopLe()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.DocAsync<XuLyNhiemVu>();

        ds.Should().NotBeEmpty();
        ds.Select(x => x.Loai).Distinct().Should().BeSubsetOf(LoaiXuLy.ToanBo());
        ds.Should().OnlyContain(x => x.MucDoHt == null
                                     || (x.MucDoHt >= GioiHan.MucDoHtMin && x.MucDoHt <= GioiHan.MucDoHtMax));
    }

    [Fact] // §4.8 / §9.8 — nhat ky goi y AI la nguon so lieu cua M13
    public async Task Seed_CoNhatKyGoiYAi()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.NhatKyAiAsync();

        ds.Should().HaveCountGreaterThanOrEqualTo(70, "§8 T9 / §9.8 can du ban ghi de tinh Precision");
        ds.Should().OnlyContain(l => l.ThuHangDaChon == null || l.ThuHangDaChon >= 1);
        ds.Should().Contain(l => l.UserIdDaChon == null, "phai co ca lan nguoi giao BO QUA goi y");
        ds.Should().Contain(l => l.ThuHangDaChon == 1, "phai co ca lan chon dung hang 1 (Precision@1)");
    }

    // =====================================================================
    // §9.8 — do hieu qua tren bo du lieu mau
    // =====================================================================

    [Fact] // §8.1 moc M4 — Precision@3 >= 0,60 (va khong duoc "hoan hao" den muc thieu thuc te)
    public async Task Metrics_Precision3_NamTrongKhoangMongDoi()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var tk = await mt.Metrics.ThongKeAiAsync(CancellationToken.None);

        tk.Precision3.Should().BeInRange(0.60, 0.80,
            "§8.1 M4 yeu cau >= 0,60; tren 0,80 la dau hieu bo mau bi lam dep qua muc");
        tk.Dat.Precision3.Should().BeTrue();
    }

    [Fact] // §9.8 — cac chi so con lai phai nhat quan voi nhau
    public async Task Metrics_CacChiSoNhatQuan()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var tk = await mt.Metrics.ThongKeAiAsync(CancellationToken.None);

        tk.SoLanGoiY.Should().BeGreaterThan(0);
        (tk.SoLanChapNhan + tk.SoLanBoQua).Should().Be(tk.SoLanGoiY);
        tk.Precision1.Should().BeInRange(0, 1);
        tk.Precision3.Should().BeInRange(0, 1);
        tk.Precision1.Should().BeLessThanOrEqualTo(tk.Precision3, "top-1 la tap con cua top-3");
        tk.Precision3.Should().BeLessThanOrEqualTo(tk.TyLeChapNhan, "top-3 la tap con cua "
            + "cac lan chon tu danh sach goi y");
        tk.Mrr.Should().BeInRange(0, 1);
        tk.GiniTai.Should().BeInRange(0, 1);
        tk.PhanBoThuHang.Should().NotBeEmpty();
        tk.PhanBoThuHang.Sum(x => x.SoLan).Should().Be(tk.SoLanGoiY);
    }

    [Fact] // §9.8 — so sanh voi 3 baseline (bat buoc co trong bao cao thuc tap)
    public async Task Metrics_CoDuBaSoSanhBaseline()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.Metrics.SoSanhBaselineAsync(CancellationToken.None);

        ds.Should().HaveCountGreaterThanOrEqualTo(3,
            "§9.8: ngau nhien / nguoi ranh nhat / chuyen mon cao nhat");
        ds.Should().OnlyContain(x => x.Precision3 >= 0 && x.Precision3 <= 1);
    }

    [Fact] // §9.8 — he so Gini cua phan bo tai phai nam trong [0, 1]
    public async Task Metrics_GiniTaiHopLe()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var gini = await mt.Metrics.GiniTaiAsync(CancellationToken.None);

        gini.Should().BeInRange(0, 1);
    }

    [Fact] // §5.8 H3 / M13 — nhat ky phan trang duoc
    public async Task Metrics_NhatKyPhanTrangDuoc()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var trang = await mt.Metrics.NhatKyAsync(1, 20, CancellationToken.None);

        trang.Trang.Should().Be(1);
        trang.KichThuoc.Should().Be(20);
        trang.Items.Should().HaveCountLessThanOrEqualTo(20);
        trang.TongSo.Should().BeGreaterThanOrEqualTo(trang.Items.Count);
        trang.TongSoTrang.Should().BeGreaterThan(0);
    }

    // =====================================================================
    // §2 / §10.5 — danh muc trang thai PHAI duoc seed
    // =====================================================================

    [Fact] // §10.5 — he goc khong co bang ma->nhan, app moi BAT BUOC seed DM_TUDIEN
    public async Task Seed_CoDanhMucTrangThaiDayDu()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var ds = await mt.DocAsync<DmTuDien>();

        var trangThaiNv = ds.Where(x => x.Type == MaTypeTuDien.TrangThaiNv).Select(x => x.Ma).ToList();
        var trangThaiPh = ds.Where(x => x.Type == MaTypeTuDien.TrangThaiPh).Select(x => x.Ma).ToList();

        trangThaiNv.Should().BeEquivalentTo(new[] { "1", "2", "3", "5", "6", "7", "13", "97" });
        trangThaiPh.Should().BeEquivalentTo(new[] { "10", "11", "12" });
        ds.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Nhan));
    }

    [Fact] // §4.7 — don vi phai tao thanh cay hop le (macha tro toi unitcode co that)
    public async Task Seed_CayDonViHopLe()
    {
        await using var mt = await MoiTruongDb.CoDuLieuMauAsync();

        var donVi = await mt.DocAsync<SysUnit>();
        var ma = donVi.Select(d => d.UnitCode).ToHashSet();

        donVi.Should().NotBeEmpty();
        donVi.Should().OnlyContain(d => d.MaCha == null || ma.Contains(d.MaCha));
        (await mt.NguoiDungAsync()).Should().OnlyContain(u => ma.Contains(u.UnitCode));
    }
}
