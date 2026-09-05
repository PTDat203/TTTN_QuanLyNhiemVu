using FluentAssertions;

using QLNV.Core.Abstractions;
using QLNV.Core.Constants;

using QLNV.Tests.HoTro;

using Xunit;

namespace QLNV.Tests;

/// <summary>
/// §6.2 — bang vai tro x hanh dong. Moi dong 6..19 co it nhat mot test bat (duoc phep)
/// va mot test chan (khong duoc phep), cong them ca truong hop tai khoan bi khoa.
///
/// §6.4: quyen tinh THUAN TUY tu trang thai + bang phan cong, KHONG dung co do FE gui len.
/// </summary>
public class QuyenTests
{
    private readonly IQuyenService _quyen = CauNoiHaTang.Quyen();

    // =====================================================================
    // §6.2 dong 6 — Sua nhiem vu da giao: userIdGiaoViec = toi VA trangthai = 3
    // =====================================================================

    [Fact]
    public void Dong6_SuaNhiemVu_NguoiGiaoVaChuaTrienKhai_DuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).SuaNhiemVu.Should().BeTrue();
    }

    [Fact]
    public void Dong6_SuaNhiemVu_KhiDaTiepNhan_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).SuaNhiemVu.Should().BeFalse();
    }

    [Fact]
    public void Dong6_SuaNhiemVu_NguoiGiaoKhac_KhongDuocPhep()
    {
        var chuNhiemVu = Xuong.NguoiGiao();
        var giaoKhac = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(chuNhiemVu.Id, TrangThaiNv.ChuaTrienKhai);

        _quyen.Tinh(nv, giaoKhac.Id, Xuong.Ctx(giaoKhac)).SuaNhiemVu.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 7 — Thu hoi nhiem vu: trangthai ∉ {1, 5, 97}
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.ChuaTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhaiQuaHan)]
    [InlineData(TrangThaiNv.TuChoi)]
    [InlineData(TrangThaiNv.GiaHan)]
    public void Dong7_ThuHoiNhiemVu_KhiChuaKetThuc_DuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).ThuHoiNhiemVu.Should().BeTrue();
    }

    [Theory]
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.HoanThanhSauHan)]
    [InlineData(TrangThaiNv.DaThuHoi)]
    public void Dong7_ThuHoiNhiemVu_KhiDaKetThuc_KhongDuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).ThuHoiNhiemVu.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 8 — Thu hoi phan cong: trangthai ∉ {1, 5}
    // =====================================================================

    [Fact]
    public void Dong8_ThuHoiPhanCong_KhiDaThuHoiNhiemVu_VanDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DaThuHoi);

        // §6.2 dong 8 chi loai {1, 5} — bam `canThuhoiPhancong` cua he goc, KHONG loai 97.
        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).ThuHoiPhanCong.Should().BeTrue();
    }

    [Fact]
    public void Dong8_ThuHoiPhanCong_KhiDaHoanThanh_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.DaXacNhan);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).ThuHoiPhanCong.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 9 — Tiep nhan: toi la CHUTRI VA trangthai = 3
    // =====================================================================

    [Fact]
    public void Dong9_TiepNhan_ChuTriVaChuaTrienKhai_DuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).TiepNhan.Should().BeTrue();
    }

    [Fact]
    public void Dong9_TiepNhan_NguoiPhoiHop_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        // §6.3 — PHOIHOP chi xem chi tiet / tai tep, khong co luong trang thai rieng.
        var q = _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.PhoiHop(nv.Id, lam.Id)));

        q.TiepNhan.Should().BeFalse();
        q.CapNhatTienDo.Should().BeFalse();
        q.GuiBaoCao.Should().BeFalse();
        q.XemChiTiet.Should().BeTrue();
        q.TaiTep.Should().BeTrue();
    }

    [Fact]
    public void Dong9_TiepNhan_KhiPhanCongDaBiThuHoi_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var pc = Xuong.ChuTri(nv.Id, lam.Id, TrangThaiPhanCong.DaThuHoi);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, pc)).TiepNhan.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 10 — Tu choi: CHUTRI VA trangthai ∈ {2,3} VA solangiahan = 0
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.ChuaTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhai)]
    public void Dong10_TuChoi_ChuTriVaTrucBNull_DuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, trucA);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).TuChoi.Should().BeTrue();
    }

    [Fact]
    public void Dong10_TuChoi_KhiDaTungGiaHan_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, soLanGiaHan: 1);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).TuChoi.Should().BeFalse();
    }

    [Fact]
    public void Dong10_TuChoi_KhiTrucBBang12_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, TrangThaiPh.TuChoi);

        // §2.4 T3 chi cho tu (3|2, null) — chan vong lap tu choi vo han qua nhanh T5.
        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).TuChoi.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 11 va 12 — Cap nhat tien do / Gui bao cao
    //   CHUTRI VA trangthai ∈ {2,3,7} VA trangthaiDvXuly ∈ {null, 12}
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.ChuaTrienKhai, null)]
    [InlineData(TrangThaiNv.DangTrienKhai, null)]
    [InlineData(TrangThaiNv.DangTrienKhaiQuaHan, null)]
    [InlineData(TrangThaiNv.DangTrienKhai, TrangThaiPh.TuChoi)]
    public void Dong11Va12_TienDoVaBaoCao_DuocPhep(int trucA, int? trucB)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, trucA, trucB);

        var q = _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id)));

        q.CapNhatTienDo.Should().BeTrue();
        q.GuiBaoCao.Should().BeTrue();
    }

    [Theory]
    [InlineData(TrangThaiNv.DangTrienKhai, TrangThaiPh.ChoXacNhan)] // dang cho nguoi giao xac nhan
    [InlineData(TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan)]     // truc A ngoai {2,3,7}
    [InlineData(TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan)]        // dang cho xu ly de nghi tu choi
    [InlineData(TrangThaiNv.DaThuHoi, null)]                        // diem cuoi §2.6
    public void Dong11Va12_TienDoVaBaoCao_KhongDuocPhep(int trucA, int? trucB)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, trucA, trucB);

        var q = _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id)));

        q.CapNhatTienDo.Should().BeFalse();
        q.GuiBaoCao.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 13 — Thu hoi bao cao: CHUTRI VA trangthai ∈ {1,5} VA trangthaiDvXuly = 10
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.HoanThanhSauHan)]
    public void Dong13_ThuHoiBaoCao_KhiChoXacNhan_DuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.ChoXacNhan);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).ThuHoiBaoCao.Should().BeTrue();
    }

    [Fact]
    public void Dong13_ThuHoiBaoCao_KhiDaNghiemThu_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.DaXacNhan);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).ThuHoiBaoCao.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 14 — Kiem tra ket qua: trangthaiDvXuly = 10 VA la nguoi giao
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.HoanThanhSauHan)]
    [InlineData(TrangThaiNv.DangTrienKhai)]
    public void Dong14_KiemTraKetQua_KhiChoXacNhan_DuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.ChoXacNhan);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).KiemTraKetQua.Should().BeTrue();
    }

    [Theory]
    [InlineData(TrangThaiNv.TuChoi)]   // (6, 10) thuoc T4/T5 chu khong phai T9/T10
    [InlineData(TrangThaiNv.DaThuHoi)] // 97 la diem cuoi §2.6
    public void Dong14_KiemTraKetQua_TrucABiLoaiTru_KhongDuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.ChoXacNhan);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).KiemTraKetQua.Should().BeFalse();
    }

    [Fact]
    public void Dong14_KiemTraKetQua_KhiChuaGuiBaoCao_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).KiemTraKetQua.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 15 — Xin gia han
    // =====================================================================

    [Theory]
    [InlineData(TrangThaiNv.ChuaTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhaiQuaHan)]
    public void Dong15_XinGiaHan_KhiDangMo_DuocPhep(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, trucA);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).XinGiaHan.Should().BeTrue();
    }

    [Fact]
    public void Dong15_XinGiaHan_KhiDaDuHaiLan_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, soLanGiaHan: GioiHan.SoLanGiaHanToiDa);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).XinGiaHan.Should().BeFalse();
    }

    [Fact]
    public void Dong15_XinGiaHan_KhiDangChoDuyet_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);

        _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id))).XinGiaHan.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 16 — Duyet / tu choi gia han
    // =====================================================================

    [Fact]
    public void Dong16_DuyetGiaHan_KhiCoDeXuatChoDuyet_DuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).DuyetGiaHan.Should().BeTrue();
    }

    [Fact]
    public void Dong16_DuyetGiaHan_KhiNhiemVuDaThuHoi_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DaThuHoi, trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);

        // §2.6 — 97 la diem cuoi, khong duoc "hoi sinh" bang duyet gia han.
        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).DuyetGiaHan.Should().BeFalse();
    }

    [Fact]
    public void Dong16_DuyetGiaHan_KhiKhongCoDeXuat_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).DuyetGiaHan.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 17 — Nhac viec: trangthai ∉ {1, 5, 97}
    // =====================================================================

    [Fact]
    public void Dong17_NhacViec_KhiDangTrienKhai_DuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).NhacViec.Should().BeTrue();
    }

    [Fact]
    public void Dong17_NhacViec_KhiDaHoanThanh_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.DaXacNhan);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).NhacViec.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 dong 18 va 19 — Xem chi tiet + Tai tep
    // =====================================================================

    [Fact]
    public void Dong18Va19_NguoiCoLienQuan_DuocXemVaTaiTep()
    {
        var giao = Xuong.NguoiGiao();
        var chuTri = Xuong.NguoiThucHien();
        var phoiHop = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var pcChuTri = Xuong.ChuTri(nv.Id, chuTri.Id);
        var pcPhoiHop = Xuong.PhoiHop(nv.Id, phoiHop.Id);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao, pcChuTri, pcPhoiHop)).XemChiTiet.Should().BeTrue();
        _quyen.Tinh(nv, chuTri.Id, Xuong.Ctx(chuTri, pcChuTri, pcPhoiHop)).XemChiTiet.Should().BeTrue();
        _quyen.Tinh(nv, phoiHop.Id, Xuong.Ctx(phoiHop, pcChuTri, pcPhoiHop)).TaiTep.Should().BeTrue();
    }

    [Fact]
    public void Dong18Va19_NguoiNgoaiCuoc_KhongDuocXem()
    {
        var giao = Xuong.NguoiGiao();
        var nguoiLa = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        var q = _quyen.Tinh(nv, nguoiLa.Id, Xuong.Ctx(nguoiLa));

        q.XemChiTiet.Should().BeFalse();
        q.TaiTep.Should().BeFalse();
    }

    // =====================================================================
    // MO RONG §2.4 T4/T5 — Xu ly de nghi tu choi (§6.2 khong co dong tuong ung)
    // =====================================================================

    [Fact]
    public void XuLyTuChoi_NguoiGiaoTaiCap_6_10_DuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).XuLyTuChoi.Should().BeTrue();
    }

    [Fact]
    public void XuLyTuChoi_KhiKhongPhaiCap_6_10_KhongDuocPhep()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.TuChoi);

        _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao)).XuLyTuChoi.Should().BeFalse();
    }

    // =====================================================================
    // §6.2 cot QUAN_TRI — bo qua dieu kien SO HUU nhung VAN theo dieu kien TRANG THAI
    // =====================================================================

    [Fact]
    public void QuanTri_BoQuaDieuKienSoHuuDuLieu()
    {
        var giao = Xuong.NguoiGiao();
        var quanTri = Xuong.QuanTri();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        var q = _quyen.Tinh(nv, quanTri.Id, Xuong.Ctx(quanTri));

        q.SuaNhiemVu.Should().BeTrue();
        q.ThuHoiNhiemVu.Should().BeTrue();
        q.NhacViec.Should().BeTrue();
        q.XemChiTiet.Should().BeTrue();
    }

    [Fact]
    public void QuanTri_VanPhaiThoaDieuKienTrangThai()
    {
        var giao = Xuong.NguoiGiao();
        var quanTri = Xuong.QuanTri();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.DaXacNhan);

        var q = _quyen.Tinh(nv, quanTri.Id, Xuong.Ctx(quanTri));

        q.SuaNhiemVu.Should().BeFalse();
        q.ThuHoiNhiemVu.Should().BeFalse();
        q.NhacViec.Should().BeFalse();
    }

    [Fact]
    public void QuanTri_KhongDuocLamThayNguoiThucHien()
    {
        var giao = Xuong.NguoiGiao();
        var quanTri = Xuong.QuanTri();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        // §6.2 dong 9, 10, 11, 12, 13, 15: cot QUAN_TRI la ❌.
        var q = _quyen.Tinh(nv, quanTri.Id, Xuong.Ctx(quanTri));

        q.TiepNhan.Should().BeFalse();
        q.TuChoi.Should().BeFalse();
        q.CapNhatTienDo.Should().BeFalse();
        q.GuiBaoCao.Should().BeFalse();
        q.ThuHoiBaoCao.Should().BeFalse();
        q.XinGiaHan.Should().BeFalse();
    }

    // =====================================================================
    // §6.4 / §9.3 dieu 1 — tai khoan bi khoa mat HET quyen
    // =====================================================================

    [Fact]
    public void TaiKhoanBiKhoa_MatToanBoQuyen()
    {
        var giao = Xuong.NguoiGiao(trangThai: 0);
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        var q = _quyen.Tinh(nv, giao.Id, Xuong.Ctx(giao));

        q.Should().Be(QuyenNhiemVu.KhongCo);
    }

    [Fact]
    public void TaiKhoanBiKhoa_ChuTriCungMatHetQuyen()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien(trangThai: 0);
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);

        var q = _quyen.Tinh(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id)));

        q.Should().Be(QuyenNhiemVu.KhongCo);
        _quyen.TinhVaiTro(nv, lam.Id, Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id)))
            .Should().Be(VaiTroNhiemVu.KhongCo);
    }

    // =====================================================================
    // §6.1 — vai tro tren nhiem vu
    // =====================================================================

    [Fact]
    public void TinhVaiTro_NhanDungChuTriVaNguoiGiao()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var pc = Xuong.ChuTri(nv.Id, lam.Id);

        var vaiGiao = _quyen.TinhVaiTro(nv, giao.Id, Xuong.Ctx(giao, pc));
        var vaiLam = _quyen.TinhVaiTro(nv, lam.Id, Xuong.Ctx(lam, pc));

        vaiGiao.LaNguoiGiao.Should().BeTrue();
        vaiGiao.LaChuTri.Should().BeFalse();
        vaiLam.LaChuTri.Should().BeTrue();
        vaiLam.LaNguoiGiao.Should().BeFalse();
        vaiLam.CoLienQuan.Should().BeTrue();
    }

    [Fact] // §6.4 — userId khong khop nguoi dang thao tac thi khong co vai tro nao
    public void TinhVaiTro_KhiUserIdKhongKhopNguoiThaoTac_TraVeKhongCo()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);

        _quyen.TinhVaiTro(nv, Guid.NewGuid(), Xuong.Ctx(giao)).Should().Be(VaiTroNhiemVu.KhongCo);
    }
}
