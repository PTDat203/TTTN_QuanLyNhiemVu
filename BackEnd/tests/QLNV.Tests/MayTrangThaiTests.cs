using FluentAssertions;

using QLNV.Core.Abstractions;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

using QLNV.Tests.HoTro;

using Xunit;

namespace QLNV.Tests;

/// <summary>
/// §8 T7 — "moi o trong ma tran T1-T14 phai co &gt;= 1 test hop le + 1 test chan",
/// dau ra kiem chung: &gt;= 25 test may trang thai pass.
///
/// T1 (Tao &amp; giao nhiem vu) KHONG nam o day: no tao ban ghi moi chu khong chuyen trang
/// thai cua ban ghi da co (xem <c>INhiemVuStateMachine</c>), duoc phu boi §5.3 C1 va
/// <c>SeedDataTests</c> (moi nhiem vu khoi tao deu o <c>(3, null)</c>).
///
/// Quy uoc: may trang thai SUA TRUC TIEP doi tuong <c>nv</c> truyen vao, nen moi khang dinh
/// deu doc thang tren <c>nv</c> sau loi goi.
/// </summary>
public class MayTrangThaiTests
{
    private readonly INhiemVuStateMachine _may = CauNoiHaTang.MayTrangThai();

    // =====================================================================
    // §2.4 T2 — Tiep nhan: (3, null) -> (2, null)
    // =====================================================================

    [Fact] // §2.4 T2 — hop le
    public void TiepNhan_TuChuaTrienKhai_ChuyenSangDangTrienKhai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TiepNhan(nv, lam.Id, new TiepNhanRequest(), ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().BeNull();
        nv.NgayTiepNhan.Should().NotBeNull("§2.4 T2 phai ghi ngaytiepnhan");
        ctx.KetXuat.LichSuXuLyMoi.Should().ContainSingle().Which.Loai.Should().Be(LoaiXuLy.TiepNhan);
    }

    [Fact] // §2.4 T2 / §6.2 dong 9 — chan: sai trang thai (da tiep nhan roi)
    public void TiepNhan_KhiDaDangTrienKhai_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TiepNhan(nv, lam.Id, null, ctx);

        kq.ThanhCong.Should().BeFalse();
        kq.Loi.Should().NotBeNullOrWhiteSpace();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    [Fact] // §6.2 dong 9 — chan: sai vai (nguoi giao khong duoc tiep nhan)
    public void TiepNhan_BoiNguoiGiao_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.TiepNhan(nv, giao.Id, null, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.ChuaTrienKhai);
    }

    // =====================================================================
    // §2.4 T3 — Tu choi nhiem vu: (3|2, null) -> (6, 10)
    // =====================================================================

    [Fact] // §2.4 T3 — hop le
    public void TuChoi_TuChuaTrienKhai_ChuyenSangTuChoiChoXacNhan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TuChoi(nv, lam.Id, new TuChoiRequest { LyDo = "Nhiệm vụ không đúng chức năng của phòng." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.TuChoi);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    [Fact] // §10.7 — rang buoc MOI: ly do tu choi BAT BUOC
    public void TuChoi_KhongCoLyDo_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TuChoi(nv, lam.Id, new TuChoiRequest { LyDo = "   " }, ctx);

        kq.ThanhCong.Should().BeFalse();
        kq.Loi.Should().NotBeNullOrWhiteSpace();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    [Fact] // §2.4 T3 — chan: truc B da khac null (chan vong lap tu choi vo han qua nhanh T5)
    public void TuChoi_KhiTrucBDaKhacNull_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, TrangThaiPh.TuChoi);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TuChoi(nv, lam.Id, new TuChoiRequest { LyDo = "Không đúng chuyên môn." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    [Fact] // §6.2 dong 10 — chan: solangiahan phai bang 0
    public void TuChoi_KhiDaTungGiaHan_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, soLanGiaHan: 1);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.TuChoi(nv, lam.Id, new TuChoiRequest { LyDo = "Không đủ nguồn lực." }, ctx);

        kq.ThanhCong.Should().BeFalse();
    }

    // =====================================================================
    // §2.4 T4/T5 — Nguoi giao xu ly de nghi tu choi
    // =====================================================================

    [Fact] // §2.4 T4 — hop le: chap nhan -> (97, null), DIEM CUOI §2.6
    public void XuLyTuChoi_ChapNhan_ChuyenSangDaThuHoi()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.XuLyTuChoi(nv, giao.Id,
            new XuLyTuChoiRequest { KetQua = KetQuaXuLyTuChoi.ChapNhan, PhanHoi = "Đồng ý, sẽ giao lại đơn vị khác." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DaThuHoi);
        nv.TrangThaiDvXuly.Should().BeNull();
        TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly).Should().Be(DiemCuoi.DaThuHoi);
    }

    [Fact] // §2.4 T5 — hop le: bac bo -> (2, 12)
    public void XuLyTuChoi_BacBo_ChuyenSangDangTrienKhaiYeuCauLamLai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.XuLyTuChoi(nv, giao.Id,
            new XuLyTuChoiRequest { KetQua = KetQuaXuLyTuChoi.BacBo, PhanHoi = "Nhiệm vụ đúng chức năng, đề nghị triển khai." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.TuChoi);
    }

    [Fact] // §2.4 T4/T5 — chan: sai vai (nguoi thuc hien khong duoc tu xu ly de nghi cua minh)
    public void XuLyTuChoi_BoiNguoiThucHien_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.XuLyTuChoi(nv, lam.Id, new XuLyTuChoiRequest { KetQua = KetQuaXuLyTuChoi.BacBo }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.TuChoi);
    }

    [Fact] // §2.4 T4/T5 — chan: ket qua ngoai tap {CHAP_NHAN, BAC_BO}
    public void XuLyTuChoi_KetQuaKhongHopLe_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.XuLyTuChoi(nv, giao.Id, new XuLyTuChoiRequest { KetQua = "KHONG_BIET" }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.TuChoi);
    }

    // =====================================================================
    // §2.4 T6 — Cap nhat tien do: KHONG doi trang thai
    // =====================================================================

    [Fact] // §2.4 T6 — hop le
    public void CapNhatTienDo_GhiMucDoHt_KhongDoiTrangThai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.CapNhatTienDo(nv, lam.Id, new TienDoRequest { MucDoHt = 40, NoiDung = "Đã hoàn thành khảo sát hiện trạng." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.MucDoHt.Should().Be(40);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai, "§2.4 T6 khong doi truc A");
        nv.TrangThaiDvXuly.Should().BeNull("§2.4 T6 khong doi truc B");
    }

    [Theory] // §10.2 — rang buoc MOI: mucdoht la so nguyen 0-100
    [InlineData(-1)]
    [InlineData(101)]
    public void CapNhatTienDo_MucDoHtNgoaiKhoang_ThatBai(int mucDoHt)
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.CapNhatTienDo(nv, lam.Id, new TienDoRequest { MucDoHt = mucDoHt }, ctx);

        kq.ThanhCong.Should().BeFalse();
        kq.Loi.Should().NotBeNullOrWhiteSpace();
        nv.MucDoHt.Should().BeNull();
    }

    [Fact] // §6.2 dong 11 — chan: dang cho xac nhan (truc B = 10) thi khong duoc cap nhat
    public void CapNhatTienDo_KhiDangChoXacNhan_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.CapNhatTienDo(nv, lam.Id, new TienDoRequest { MucDoHt = 50 }, ctx);

        kq.ThanhCong.Should().BeFalse();
    }

    // =====================================================================
    // §2.4 T7 — Gui bao cao ket qua: (2|3|7, null|12) -> (da chon, 10)
    // =====================================================================

    [Fact] // §2.4 T7 + §5.4 D5 — hop le: con han duoc chon ma 1
    public void GuiBaoCao_ConHan_ChonHoanThanh_ChuyenSangChoXacNhan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.GuiBaoCao(nv, lam.Id,
            new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanh, NoiDung = "Đã hoàn thành và gửi kèm báo cáo." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.HoanThanh);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    [Fact] // §2.4 T7 + §5.4 D5 — hop le: qua han duoc chon ma 5
    public void GuiBaoCao_QuaHan_ChonHoanThanhSauHan_ChuyenSangChoXacNhan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhaiQuaHan, hanXuLyTh: Xuong.HanQuaHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.GuiBaoCao(nv, lam.Id,
            new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanhSauHan, NoiDung = "Hoàn thành nhưng chậm so với hạn." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.HoanThanhSauHan);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    [Fact] // §5.4 D4/D5 — chan: qua han ma chon ma 1 (server phai validate lai, §6.4)
    public void GuiBaoCao_QuaHan_ChonHoanThanhTrongHan_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhaiQuaHan, hanXuLyTh: Xuong.HanQuaHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.GuiBaoCao(nv, lam.Id,
            new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanh, NoiDung = "Hoàn thành." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhaiQuaHan);
        nv.TrangThaiDvXuly.Should().BeNull();
    }

    [Fact] // §1.2 buoc 5 — chan: ma 13 LUON bi loai khoi combobox ket qua xu ly
    public void GuiBaoCao_ChonMaGiaHan13_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.GuiBaoCao(nv, lam.Id, new BaoCaoRequest { TrangThai = TrangThaiNv.GiaHan, NoiDung = "Xin gia hạn." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    [Fact] // §1.2 buoc 5 + §4.4 — noi dung bao cao BAT BUOC khi chon ma 1 hoac 5
    public void GuiBaoCao_ChonHoanThanhMaThieuNoiDung_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.GuiBaoCao(nv, lam.Id, new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanh, NoiDung = "" }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThaiDvXuly.Should().BeNull();
    }

    // =====================================================================
    // §2.4 T8 — Thu hoi bao cao: (1|5, 10) -> (2 con han / 7 qua han, null)
    // =====================================================================

    [Fact] // §2.4 T8 + §2.5 — hop le: con han -> 2
    public void ThuHoiBaoCao_ConHan_VeDangTrienKhai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.ThuHoiBaoCao(nv, lam.Id, new ThuHoiBaoCaoRequest { LyDo = "Thiếu số liệu, xin bổ sung." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().BeNull();
    }

    [Fact] // §2.4 T8 + §2.5 — hop le: qua han -> 7
    public void ThuHoiBaoCao_QuaHan_VeDangTrienKhaiQuaHan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanhSauHan, TrangThaiPh.ChoXacNhan, Xuong.HanQuaHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.ThuHoiBaoCao(nv, lam.Id, null, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhaiQuaHan);
        nv.TrangThaiDvXuly.Should().BeNull();
    }

    [Fact] // §6.2 dong 13 — chan: chua gui bao cao thi khong co gi de thu hoi
    public void ThuHoiBaoCao_KhiChuaGuiBaoCao_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.ThuHoiBaoCao(nv, lam.Id, null, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    // =====================================================================
    // §2.4 T9/T10 — Kiem tra ket qua (nghiem thu)
    // =====================================================================

    [Theory] // §2.4 T9 — hop le: DAT GIU NGUYEN truc A (1 van la 1, 5 van la 5), khong ep ve 1
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.HoanThanhSauHan)]
    public void NghiemThu_KhiDat_GiuNguyenTrucA(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Kết quả đạt yêu cầu." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(trucA, "§2.4 T9 ghi ro (giu nguyen, 11)");
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.DaXacNhan);
        nv.NgayHoanThanhThucTe.Should().NotBeNull();
        TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly).Should().Be(DiemCuoi.HoanThanhNghiemThu);
    }

    [Fact] // §2.4 T10 + §2.5 — hop le: CHUA_DAT tu (1, 10) con han -> (2, 12)
    public void NghiemThu_ChuaDat_Tu_1_10_ConHan_VeDangTrienKhai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.ChuaDat, PhanHoi = "Thiếu phụ lục số liệu, đề nghị bổ sung." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.TuChoi);
        TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly).Should().BeNull();
    }

    [Fact] // §2.4 T10 + §2.5 — hop le: CHUA_DAT tu (1, 10) qua han -> (7, 12)
    public void NghiemThu_ChuaDat_Tu_1_10_QuaHan_VeDangTrienKhaiQuaHan()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanQuaHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.ChuaDat, PhanHoi = "Chưa đạt yêu cầu, làm lại." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhaiQuaHan);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.TuChoi);
    }

    [Fact] // §2.4 T10 — hop le: CHUA_DAT tu (2, 10) CHI dat truc B = 12, KHONG doi truc A
    public void NghiemThu_ChuaDat_Tu_2_10_KhongDoiTrucA()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.ChuaDat, PhanHoi = "Tiến độ chưa đạt, tiếp tục triển khai." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai, "truc A khong nam trong {1,5} nen giu nguyen");
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.TuChoi);
    }

    [Theory] // §2.4 T9 — chan: khong duoc cham DAT khi truc A chua thuoc {1, 5} (tranh cap chet)
    [InlineData(TrangThaiNv.DangTrienKhai)]
    [InlineData(TrangThaiNv.ChuaTrienKhai)]
    [InlineData(TrangThaiNv.DangTrienKhaiQuaHan)]
    public void NghiemThu_Dat_KhiTrucAChuaHoanThanh_BiChan(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Đạt." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan, "khong duoc sinh cap (2|3|7, 11)");
    }

    [Fact] // §6.2 dong 14 — chan: cap (6, 10) thuoc T4/T5 chu khong phai T9/T10
    public void NghiemThu_KhiTrucABangTuChoi6_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.ChuaDat, PhanHoi = "Yêu cầu làm lại." }, ctx);

        kq.ThanhCong.Should().BeFalse("khong duoc sinh cap chet (6, 11) / (6, 12) ngoai §2.4");
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    [Fact] // §2.6 — chan: nhiem vu da thu hoi (97) la DIEM CUOI, khong nghiem thu duoc
    public void NghiemThu_KhiDaThuHoi97_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DaThuHoi, TrangThaiPh.ChoXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Đạt." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DaThuHoi);
    }

    [Fact] // §1.2 buoc 6 + §10.7 — noi dung phan hoi BAT BUOC
    public void NghiemThu_KhongCoPhanHoi_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id, new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "  " }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    [Fact] // §5.5 E1 — he so chat luong phai nam trong 1..6
    public void NghiemThu_HeSoChatLuongNgoaiKhoang_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Đạt.", HsChatLuong = 7 }, ctx);

        kq.ThanhCong.Should().BeFalse();
    }

    [Fact] // §6.2 dong 14 — chan: sai vai (nguoi thuc hien khong duoc tu nghiem thu)
    public void NghiemThu_BoiNguoiThucHien_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.NghiemThu(nv, lam.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Tự nghiệm thu." }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.ChoXacNhan);
    }

    // =====================================================================
    // §2.4 T11 — Xin gia han
    // =====================================================================

    [Fact] // §2.4 T11 — hop le
    public void XinGiaHan_TuDangTrienKhai_ChuyenSangGiaHanChoDuyet()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.XinGiaHan(nv, lam.Id,
            new GiaHanRequest { HanXuLyDeXuat = new DateOnly(2026, 10, 15), NoiDung = "Chờ ý kiến của đơn vị phối hợp." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.GiaHan);
        nv.TrangThaiXuLyGiaHan.Should().Be(TrangThaiGiaHan.ChoDuyet);
        nv.SoLanGiaHan.Should().Be(0, "§2.3 solangiahan chi tang khi DUYET");
        ctx.KetXuat.GiaHanMoi.Should().ContainSingle()
            .Which.TrangThaiCu.Should().Be(TrangThaiNv.DangTrienKhai, "phai nho truc A cu de khoi phuc o T12/T13");
    }

    [Fact] // §3.3 M10 — chan: han de xuat phai MUON HON han hien tai
    public void XinGiaHan_HanDeXuatKhongMuonHon_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.XinGiaHan(nv, lam.Id, new GiaHanRequest { HanXuLyDeXuat = Xuong.HanConHan }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        ctx.KetXuat.GiaHanMoi.Should().BeEmpty();
    }

    [Fact] // §2.3 / §6.2 dong 15 — chan: solangiahan < 2
    public void XinGiaHan_KhiDaGiaHanDuHaiLan_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan, soLanGiaHan: 2);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.XinGiaHan(nv, lam.Id, new GiaHanRequest { HanXuLyDeXuat = new DateOnly(2026, 10, 15) }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    [Fact] // §6.2 dong 15 — chan: dang co de xuat cho duyet (truc C = 10)
    public void XinGiaHan_KhiDangChoDuyet_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.XinGiaHan(nv, lam.Id, new GiaHanRequest { HanXuLyDeXuat = new DateOnly(2026, 11, 15) }, ctx);

        kq.ThanhCong.Should().BeFalse();
    }

    // =====================================================================
    // §2.4 T12/T13 — Duyet / tu choi gia han
    // =====================================================================

    [Fact] // §2.4 T12 + §5.6 F2 — hop le
    public void DuyetGiaHan_Duyet_TangSoLanGiaHanVaCapNhatHan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var hanMoi = new DateOnly(2026, 10, 15);
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var deXuat = Xuong.DeXuatGiaHan(nv.Id, lam.Id, hanMoi, Xuong.HanConHan, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao) with { GiaHanChoDuyet = deXuat };

        var kq = _may.DuyetGiaHan(nv, giao.Id,
            new DuyetGiaHanRequest { KetQua = KetQuaDuyetGiaHan.Duyet, PhanHoi = "Đồng ý gia hạn." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThaiXuLyGiaHan.Should().Be(TrangThaiGiaHan.DaDuyet);
        nv.SoLanGiaHan.Should().Be(1, "§2.3 — duyet thi +1");
        nv.HanXuLyTh.Should().Be(hanMoi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai, "khoi phuc truc A tu trangthai_cu, han moi con hieu luc");
        deXuat.TrangThai.Should().Be(TrangThaiGiaHan.DaDuyet);
    }

    [Fact] // §2.4 T13 — hop le: tu choi gia han, han giu nguyen
    public void DuyetGiaHan_TuChoi_GiuNguyenHanVaKhongTangSoLan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var deXuat = Xuong.DeXuatGiaHan(nv.Id, lam.Id, new DateOnly(2026, 10, 15), Xuong.HanConHan, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao) with { GiaHanChoDuyet = deXuat };

        var kq = _may.DuyetGiaHan(nv, giao.Id,
            new DuyetGiaHanRequest { KetQua = KetQuaDuyetGiaHan.TuChoi, PhanHoi = "Không đồng ý, đề nghị hoàn thành đúng hạn." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThaiXuLyGiaHan.Should().Be(TrangThaiGiaHan.TuChoi);
        nv.SoLanGiaHan.Should().Be(0);
        nv.HanXuLyTh.Should().Be(Xuong.HanConHan);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        deXuat.TrangThai.Should().Be(TrangThaiGiaHan.TuChoi);
    }

    [Fact] // §2.4 T12 — hop le: truoc do CHUA TIEP NHAN (3) thi khong duoc nhay sang 2
    public void DuyetGiaHan_KhiTruocDoChuaTiepNhan_GiuNguyenChuaTrienKhai()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var hanMoi = new DateOnly(2026, 10, 15);
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        nv.NgayTiepNhan = null;
        var deXuat = Xuong.DeXuatGiaHan(nv.Id, lam.Id, hanMoi, Xuong.HanConHan, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(giao) with { GiaHanChoDuyet = deXuat };

        var kq = _may.DuyetGiaHan(nv, giao.Id, new DuyetGiaHanRequest { KetQua = KetQuaDuyetGiaHan.Duyet }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.ChuaTrienKhai, "khong duoc bo qua buoc Tiep nhan §2.4 T2");
        nv.NgayTiepNhan.Should().BeNull();
    }

    [Fact] // §2.6 — chan: khong duoc "hoi sinh" nhiem vu da thu hoi (97) bang cach duyet gia han
    public void DuyetGiaHan_KhiNhiemVuDaThuHoi_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DaThuHoi, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var deXuat = Xuong.DeXuatGiaHan(nv.Id, lam.Id, new DateOnly(2026, 10, 15), Xuong.HanConHan, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao) with { GiaHanChoDuyet = deXuat };

        var kq = _may.DuyetGiaHan(nv, giao.Id, new DuyetGiaHanRequest { KetQua = KetQuaDuyetGiaHan.Duyet }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DaThuHoi, "97 la DIEM CUOI §2.6");
        nv.SoLanGiaHan.Should().Be(0);
    }

    [Fact] // §2.4 T12/T13 — chan: khong co de xuat nao dang cho duyet
    public void DuyetGiaHan_KhongCoDeXuatChoDuyet_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.DuyetGiaHan(nv, giao.Id, new DuyetGiaHanRequest { KetQua = KetQuaDuyetGiaHan.Duyet }, ctx);

        kq.ThanhCong.Should().BeFalse();
        kq.Loi.Should().NotBeNullOrWhiteSpace();
    }

    // =====================================================================
    // §2.4 T14 — Thu hoi nhiem vu
    // =====================================================================

    [Fact] // §2.4 T14 — hop le
    public void ThuHoiNhiemVu_ChuyenSangDaThuHoi()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.ThuHoiNhiemVu(nv, giao.Id, new ThuHoiNhiemVuRequest { LyDo = "Nhiệm vụ không còn cần thiết." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DaThuHoi);
        nv.TrangThaiDvXuly.Should().BeNull();
        TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly).Should().Be(DiemCuoi.DaThuHoi);
    }

    [Fact] // §2.4 T14 — hop le: phai DONG moi de xuat gia han con treo, neu khong 97 bi "hoi sinh"
    public void ThuHoiNhiemVu_DongMoiDeXuatGiaHanConTreo()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.GiaHan, hanXuLyTh: Xuong.HanConHan,
            trangThaiXuLyGiaHan: TrangThaiGiaHan.ChoDuyet);
        var deXuat = Xuong.DeXuatGiaHan(nv.Id, lam.Id, new DateOnly(2026, 10, 15), Xuong.HanConHan, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao) with { GiaHanChoDuyet = deXuat };

        var kq = _may.ThuHoiNhiemVu(nv, giao.Id, null, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DaThuHoi);
        nv.TrangThaiXuLyGiaHan.Should().Be(TrangThaiGiaHan.TuChoi);
        deXuat.TrangThai.Should().Be(TrangThaiGiaHan.TuChoi);
        ctx.KetXuat.GiaHanCapNhat.Should().Contain(deXuat);
    }

    [Theory] // §6.2 dong 7 — chan: trangthai ∈ {1, 5, 97} thi khong thu hoi duoc
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.HoanThanhSauHan)]
    [InlineData(TrangThaiNv.DaThuHoi)]
    public void ThuHoiNhiemVu_KhiDaKetThuc_BiChan(int trucA)
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, trucA, TrangThaiPh.DaXacNhan);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.ThuHoiNhiemVu(nv, giao.Id, null, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(trucA);
    }

    [Fact] // §6.2 dong 7 — chan: sai vai
    public void ThuHoiNhiemVu_BoiNguoiThucHien_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        var kq = _may.ThuHoiNhiemVu(nv, lam.Id, null, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    // =====================================================================
    // §1.3 — Nhac viec (nhanh phu GIU LAI)
    // =====================================================================

    [Fact] // §1.3 + §10.8 — hop le: chi ghi lich su, KHONG doi trang thai
    public void NhacViec_ChiGhiLichSu_KhongDoiTrangThai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NhacViec(nv, giao.Id, new NhacViecRequest { NoiDung = "Đề nghị đẩy nhanh tiến độ." }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().BeNull();
        ctx.KetXuat.LichSuXuLyMoi.Should().ContainSingle().Which.Loai.Should().Be(LoaiXuLy.NhacViec);
    }

    [Fact] // §1.3 — chan: noi dung nhac viec BAT BUOC
    public void NhacViec_KhongCoNoiDung_ThatBai()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.NhacViec(nv, giao.Id, new NhacViecRequest { NoiDung = "" }, ctx);

        kq.ThanhCong.Should().BeFalse();
        ctx.KetXuat.LichSuXuLyMoi.Should().BeEmpty();
    }

    // =====================================================================
    // §5.3 C5 / §6.2 dong 6 — Sua nhiem vu da giao
    // =====================================================================

    [Fact] // §6.2 dong 6 — hop le: chi khi trangthai = 3
    public void SuaNhiemVu_KhiChuaTrienKhai_ThanhCong()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(giao);

        var kq = _may.SuaNhiemVu(nv, giao.Id, new SuaNhiemVuRequest
        {
            NoiDung = "Nội dung nhiệm vụ đã được điều chỉnh theo chỉ đạo mới.",
            DoKhan = DoKhan.TrongTam,
            LinhVuc = "CNTT_PM",
            HanXuLyTh = new DateOnly(2026, 10, 1)
        }, ctx);

        kq.ThanhCong.Should().BeTrue(kq.Loi);
        nv.NoiDung.Should().Contain("điều chỉnh");
        nv.DoKhan.Should().Be(DoKhan.TrongTam);
        nv.TrangThai.Should().Be(TrangThaiNv.ChuaTrienKhai);
    }

    [Fact] // §6.2 dong 6 — chan: da tiep nhan thi khong sua duoc
    public void SuaNhiemVu_KhiDaTiepNhan_BiChan()
    {
        var giao = Xuong.NguoiGiao();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.DangTrienKhai);
        var noiDungCu = nv.NoiDung;
        var ctx = Xuong.Ctx(giao);

        var kq = _may.SuaNhiemVu(nv, giao.Id, new SuaNhiemVuRequest
        {
            NoiDung = "Sửa trộm sau khi đã giao.",
            DoKhan = DoKhan.DotXuat
        }, ctx);

        kq.ThanhCong.Should().BeFalse();
        nv.NoiDung.Should().Be(noiDungCu);
    }

    // =====================================================================
    // §2.6 — Diem cuoi (cac cap DE NHAM)
    // =====================================================================

    [Theory] // §2.6 — CHI (1|5, 11) va 97 la diem cuoi
    [InlineData(TrangThaiNv.HoanThanh, TrangThaiPh.DaXacNhan, DiemCuoi.HoanThanhNghiemThu)]
    [InlineData(TrangThaiNv.HoanThanhSauHan, TrangThaiPh.DaXacNhan, DiemCuoi.HoanThanhNghiemThu)]
    [InlineData(TrangThaiNv.DaThuHoi, null, DiemCuoi.DaThuHoi)]
    public void DiemCuoi_NhanDungHaiTruongHopKetThuc(int trucA, int? trucB, string mong)
        => TrangThaiNv.LaDiemCuoi(trucA, trucB).Should().Be(mong);

    [Theory] // §2.6 — cac cap KHONG phai diem cuoi (bam goc, de nham)
    [InlineData(TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan)]        // (1, 10) — van thu hoi bao cao duoc
    [InlineData(TrangThaiNv.HoanThanhSauHan, TrangThaiPh.ChoXacNhan)]  // (5, 10)
    [InlineData(TrangThaiNv.HoanThanh, TrangThaiPh.TuChoi)]            // (1, 12) — vong yeu cau bo sung
    [InlineData(TrangThaiNv.HoanThanhSauHan, TrangThaiPh.TuChoi)]      // (5, 12)
    [InlineData(TrangThaiNv.TuChoi, TrangThaiPh.TuChoi)]               // (6, 12) — van xu ly tiep duoc
    public void DiemCuoi_CacCapDeNham_KhongPhaiDiemCuoi(int trucA, int trucB)
        => TrangThaiNv.LaDiemCuoi(trucA, trucB).Should().BeNull();

    // =====================================================================
    // §2.5 / §5.9 I5 — Job tu dong theo han
    // =====================================================================

    [Theory] // §2.5 — 2 -> 7 va 3 -> 7 khi qua han
    [InlineData(TrangThaiNv.DangTrienKhai)]
    [InlineData(TrangThaiNv.ChuaTrienKhai)]
    public void ApQuyTacQuaHan_ChuyenSangDangTrienKhaiQuaHan(int trucA)
    {
        var nv = Xuong.NhiemVu(Guid.NewGuid(), trucA, hanXuLyTh: Xuong.HanQuaHan);

        var doi = _may.ApQuyTacQuaHan(nv, Xuong.HomNay);

        doi.Should().BeTrue();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhaiQuaHan);
    }

    [Theory] // §2.5 — chi ap cho 2 va 3; cac ma khac giu nguyen
    [InlineData(TrangThaiNv.HoanThanh)]
    [InlineData(TrangThaiNv.TuChoi)]
    [InlineData(TrangThaiNv.GiaHan)]
    [InlineData(TrangThaiNv.DaThuHoi)]
    [InlineData(TrangThaiNv.DangTrienKhaiQuaHan)]
    public void ApQuyTacQuaHan_KhongApChoTrangThaiKhac(int trucA)
    {
        var nv = Xuong.NhiemVu(Guid.NewGuid(), trucA, hanXuLyTh: Xuong.HanQuaHan);

        var doi = _may.ApQuyTacQuaHan(nv, Xuong.HomNay);

        doi.Should().BeFalse();
        nv.TrangThai.Should().Be(trucA);
    }

    [Fact] // §2.5 — con han thi khong doi gi
    public void ApQuyTacQuaHan_KhiConHan_KhongDoi()
    {
        var nv = Xuong.NhiemVu(Guid.NewGuid(), TrangThaiNv.DangTrienKhai, hanXuLyTh: Xuong.HanConHan);

        _may.ApQuyTacQuaHan(nv, Xuong.HomNay).Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
    }

    // =====================================================================
    // §6.4 — tai khoan bi khoa mat het quyen (kiem o tang may trang thai)
    // =====================================================================

    [Fact] // §9.3 dieu 1 + §6.4 — tai khoan khoa khong lam duoc gi
    public void MoiHanhDong_KhiTaiKhoanBiKhoa_DeuBiChan()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien(trangThai: 0);
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai);
        var ctx = Xuong.Ctx(lam, Xuong.ChuTri(nv.Id, lam.Id));

        _may.TiepNhan(nv, lam.Id, null, ctx).ThanhCong.Should().BeFalse();
        _may.TuChoi(nv, lam.Id, new TuChoiRequest { LyDo = "Không nhận." }, ctx).ThanhCong.Should().BeFalse();
        nv.TrangThai.Should().Be(TrangThaiNv.ChuaTrienKhai);
    }

    // =====================================================================
    // Vong doi day du — §8 M3 "di het so do, co ca nhanh Chua dat quay lai"
    // =====================================================================

    [Fact] // §1.2 buoc 3 -> 6: Tiep nhan -> Tien do -> Bao cao -> CHUA DAT -> Bao cao lai -> DAT
    public void VongDoiDayDu_TuTiepNhanDenNghiemThuDat()
    {
        var giao = Xuong.NguoiGiao();
        var lam = Xuong.NguoiThucHien();
        var nv = Xuong.NhiemVu(giao.Id, TrangThaiNv.ChuaTrienKhai, hanXuLyTh: Xuong.HanConHan);
        var pc = Xuong.ChuTri(nv.Id, lam.Id);
        var ctxLam = Xuong.Ctx(lam, pc);
        var ctxGiao = Xuong.Ctx(giao, pc);

        _may.TiepNhan(nv, lam.Id, null, ctxLam).ThanhCong.Should().BeTrue();
        _may.CapNhatTienDo(nv, lam.Id, new TienDoRequest { MucDoHt = 60 }, ctxLam).ThanhCong.Should().BeTrue();
        _may.GuiBaoCao(nv, lam.Id,
            new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanh, NoiDung = "Báo cáo lần 1." }, ctxLam)
            .ThanhCong.Should().BeTrue();

        _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.ChuaDat, PhanHoi = "Bổ sung phụ lục." }, ctxGiao)
            .ThanhCong.Should().BeTrue();
        nv.TrangThai.Should().Be(TrangThaiNv.DangTrienKhai);
        nv.TrangThaiDvXuly.Should().Be(TrangThaiPh.TuChoi);

        _may.GuiBaoCao(nv, lam.Id,
            new BaoCaoRequest { TrangThai = TrangThaiNv.HoanThanh, NoiDung = "Báo cáo lần 2, đã bổ sung." }, ctxLam)
            .ThanhCong.Should().BeTrue("§6.2 dong 12 cho phep bao cao lai khi truc B = 12");

        _may.NghiemThu(nv, giao.Id,
            new NghiemThuRequest { KetQua = KetQuaNghiemThu.Dat, PhanHoi = "Đạt yêu cầu.", HsChatLuong = 5 }, ctxGiao)
            .ThanhCong.Should().BeTrue();

        TrangThaiNv.LaDiemCuoi(nv.TrangThai, nv.TrangThaiDvXuly).Should().Be(DiemCuoi.HoanThanhNghiemThu);
        nv.HsChatLuong.Should().Be(5);
    }
}
