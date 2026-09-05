using FluentAssertions;

using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

using QLNV.Tests.HoTro;

using Xunit;

namespace QLNV.Tests;

/// <summary>
/// §9.3 — §9.6 — dong co goi y nguoi thuc hien.
///
/// Cach kiem: goi thang hop dong H1 (<c>IRecommendationService.GoiYAsync</c>) roi doc
/// <c>diemThanhPhan</c> — theo §9.6 moi thanh phan tra ve dung gia tri <c>S1..S5</c>
/// (da lam tron 2 chu so), nen kiem duoc tung cong thuc cua §9.4 ma khong pha vo dong goi.
///
/// Moi kich ban deu dung CSDL trong bo nho rieng, khong dung <c>DbInitializer</c>,
/// de con so kiem thu khong phu thuoc bo du lieu mau.
/// </summary>
public class AiEngineTests
{
    /// <summary>Nguoi giao co dinh — moi nhiem vu lich su deu do nguoi nay giao.</summary>
    private static readonly Guid NguoiGiaoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string LinhVucChinh = "CNTT_PM";   // nhom cha: CNTT
    private const string LinhVucAnhEm = "CNTT_HT";   // cung nhom cha CNTT
    private const string LinhVucKhacNhom = "HC_VT";  // nhom cha: HC

    // =====================================================================
    // Dung kich ban
    // =====================================================================

    /// <summary>Moi truong rong da co don vi + danh muc linh vuc phan cap (§9.4 S1 nhom cha).</summary>
    private static async Task<MoiTruongDb> TaoMoiTruongAsync()
    {
        var mt = MoiTruongDb.Rong();
        mt.Them(
            new SysUnit { UnitCode = "P01", TenDonVi = "Phòng Công nghệ thông tin", CapDonVi = 2, TrangThai = 1 },
            new DmLinhVuc { Ma = "CNTT", Ten = "Công nghệ thông tin", TrangThai = 1, ThuTu = 1 },
            new DmLinhVuc { Ma = LinhVucChinh, Ten = "Phần mềm", NhomCha = "CNTT", TrangThai = 1, ThuTu = 2 },
            new DmLinhVuc { Ma = LinhVucAnhEm, Ten = "Hạ tầng", NhomCha = "CNTT", TrangThai = 1, ThuTu = 3 },
            new DmLinhVuc { Ma = "HC", Ten = "Hành chính", TrangThai = 1, ThuTu = 4 },
            new DmLinhVuc { Ma = LinhVucKhacNhom, Ten = "Văn thư", NhomCha = "HC", TrangThai = 1, ThuTu = 5 });
        await mt.LuuAsync();
        return mt;
    }

    private static SysUser ThemNguoiThucHien(MoiTruongDb mt, string hoTen, int maxConcurrentTasks = 8)
    {
        var u = Xuong.NguoiThucHien(maxConcurrentTasks: maxConcurrentTasks);
        u.FullName = hoTen;
        mt.Them(u);
        return u;
    }

    /// <summary>
    /// Them <paramref name="soLuong"/> nhiem vu DA NGHIEM THU (§2.6: truc A ∈ {1,5} va truc B = 11)
    /// cho <paramref name="u"/> voi vai CHUTRI. <paramref name="soDungHan"/> ban ghi dau dung han (ma 1).
    /// </summary>
    private static void ThemViecDaNghiemThu(
        MoiTruongDb mt, SysUser u, string? linhVuc, int soLuong, int soDungHan, int soViecCoGiaHan = 0)
    {
        for (var i = 0; i < soLuong; i++)
        {
            var trucA = i < soDungHan ? TrangThaiNv.HoanThanh : TrangThaiNv.HoanThanhSauHan;
            var nv = Xuong.NhiemVu(NguoiGiaoId, trucA, TrangThaiPh.DaXacNhan, Xuong.HanConHan,
                soLanGiaHan: i < soViecCoGiaHan ? 1 : 0, linhVuc: linhVuc);
            mt.Them(nv, Xuong.ChuTri(nv.Id, u.Id));
        }
    }

    /// <summary>Them nhiem vu DANG GIU (§4.8 view: truc A ∉ {1,5,97}) de tao tai cho §9.4 S4.</summary>
    private static void ThemViecDangGiu(
        MoiTruongDb mt, SysUser u, int soLuong,
        string doKhan = DoKhan.ThuongXuyen,
        int trucA = TrangThaiNv.DangTrienKhai)
    {
        for (var i = 0; i < soLuong; i++)
        {
            var han = trucA == TrangThaiNv.DangTrienKhaiQuaHan ? Xuong.HanQuaHan : Xuong.HanConHan;
            var nv = Xuong.NhiemVu(NguoiGiaoId, trucA, null, han, linhVuc: LinhVucChinh, doKhan: doKhan);
            mt.Them(nv, Xuong.ChuTri(nv.Id, u.Id));
        }
    }

    /// <summary>Nguoi lam "moc": bao dam he thong CO du lieu nghiem thu =&gt; che do DAY_DU (§9.5).</summary>
    private static SysUser ThemNguoiMoc(MoiTruongDb mt)
    {
        var u = ThemNguoiThucHien(mt, "Zz Người mốc dữ liệu");
        ThemViecDaNghiemThu(mt, u, LinhVucKhacNhom, 4, 3);
        return u;
    }

    private static GoiYRequest YeuCau(string? linhVuc = LinhVucChinh, int soLuong = 50) => new()
    {
        NoiDung = "Xây dựng phần mềm quản lý nhiệm vụ cho đơn vị.",
        LinhVuc = linhVuc,
        DoKhan = DoKhan.ThuongXuyen,
        HanXuLyTh = Xuong.HanConHan,
        SoLuong = soLuong
    };

    private static UngVienDto Tim(GoiYResponse kq, SysUser u)
        => kq.UngVien.Single(x => x.UserId == u.Id);

    // =====================================================================
    // §9.4 S1 — Chuyen mon
    // =====================================================================

    [Theory] // §9.4 S1: S1_chinh = min(1, n / 5) — n=0 -> 0,00 · n=3 -> 0,60 · n=5 -> 1,00
    [InlineData(0, 0.00)]
    [InlineData(3, 0.60)]
    [InlineData(5, 1.00)]
    [InlineData(7, 1.00)]
    public async Task S1_ChuyenMon_TinhTheoSoViecDaNghiemThuTrongLinhVuc(int n, double mong)
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "An Chuyên môn");
        ThemViecDaNghiemThu(mt, u, LinhVucChinh, n, n);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.ChuyenMon.Diem.Should().BeApproximately(mong, 0.01);
    }

    [Fact] // §9.4 S1: n = 0 nhung co 5 viec CUNG NHOM CHA -> chiet khau 50% => 0,50
    public async Task S1_ChiCoViecCungNhomCha_ChietKhauConMotNua()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Bình Nhóm cha");
        ThemViecDaNghiemThu(mt, u, LinhVucAnhEm, 5, 5);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.ChuyenMon.Diem.Should().BeApproximately(0.50, 0.01);
    }

    [Fact] // §9.4 S1: viec o linh vuc KHAC NHOM CHA khong duoc tinh cho S1
    public async Task S1_ViecKhacNhomCha_KhongDuocTinh()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Cường Khác nhóm");
        ThemViecDaNghiemThu(mt, u, LinhVucKhacNhom, 5, 5);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.ChuyenMon.Diem.Should().BeApproximately(0.00, 0.01);
    }

    [Fact] // §9.5 — nhiem vu KHONG co linh vuc: S1 tinh tren TONG so viec da nghiem thu
    public async Task S1_NhiemVuKhongCoLinhVuc_TinhTrenTongSoViec()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Dũng Không lĩnh vực");
        ThemViecDaNghiemThu(mt, u, LinhVucKhacNhom, 5, 5);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(linhVuc: null), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.ChuyenMon.Diem.Should().BeApproximately(1.00, 0.01);
        kq.CanhBao.Should().NotBeEmpty("§9.5 phai canh bao khi nhiem vu chua gan linh vuc");
    }

    // =====================================================================
    // §9.4 S2 — Lich su thuc hien: ln(1+N) / ln(1+10)
    // =====================================================================

    [Theory] // N=0 -> 0,00 · N=3 -> 0,58 · N=5 -> 0,75 · N=10 -> 1,00 · N=30 -> 1,00 (cat nguong)
    [InlineData(0, 0.00)]
    [InlineData(3, 0.58)]
    [InlineData(5, 0.75)]
    [InlineData(10, 1.00)]
    [InlineData(15, 1.00)]
    public async Task S2_LichSu_TinhTheoLogaritTongSoViec(int soViec, double mong)
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Em Lịch sử");
        ThemViecDaNghiemThu(mt, u, LinhVucChinh, soViec, soViec);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.LichSu.Diem.Should().BeApproximately(mong, 0.01);
    }

    // =====================================================================
    // §9.4 S3 — Hieu qua cong viec (lam muot Laplace)
    // =====================================================================

    [Fact] // §9.5 dong 1 — nguoi moi (0 viec): S3 = p0 = 0,70 (hsChatluong DA BI LOAI khoi cong thuc)
    public async Task S3_NguoiMoi_BangTienNghiemP0()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Giang Người mới");
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var uv = Tim(kq, u);

        uv.DiemThanhPhan.HieuQua.Diem.Should().BeApproximately(0.70, 0.01);
        uv.SoLieu.SoNvHoanThanh.Should().Be(0);
        uv.Nhan.Select(n => n.Ma).Should().Contain(MaNhanUngVien.NguoiMoi);
    }

    [Fact] // §9.4 S3 (a) — r_dunghan = (so_dunghan + 5 x 0,70) / (so_hoanthanh + 5)
    public async Task S3_TatCaDungHan_TienDanNhungKhongDatMotTuyetDoi()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Hà Đúng hạn");
        ThemViecDaNghiemThu(mt, u, LinhVucChinh, 10, 10);   // 10/10 dung han
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        // (10 + 5 x 0,70) / (10 + 5) = 13,5 / 15 = 0,90
        Tim(kq, u).DiemThanhPhan.HieuQua.Diem.Should().BeApproximately(0.90, 0.01);
    }

    [Fact] // §9.4 S3 (c) — phat gia han: 0,15 x (Σ solangiahan / max(1, so_hoanthanh))
    public async Task S3_CoGiaHan_BiTruDiem()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var sach = ThemNguoiThucHien(mt, "Ia Sạch");
        var giaHan = ThemNguoiThucHien(mt, "Ib Hay gia hạn");
        ThemViecDaNghiemThu(mt, sach, LinhVucChinh, 10, 10);
        ThemViecDaNghiemThu(mt, giaHan, LinhVucChinh, 10, 10, soViecCoGiaHan: 5);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        Tim(kq, giaHan).SoLieu.SoLanGiaHan.Should().Be(5);

        Tim(kq, giaHan).DiemThanhPhan.HieuQua.Diem
            .Should().BeLessThan(Tim(kq, sach).DiemThanhPhan.HieuQua.Diem);
    }

    // =====================================================================
    // §9.4 S4 — Khoi luong hien tai: S4 = clamp(0, 1, 1 - tai / K)
    // =====================================================================

    [Fact] // tai = 0 -> S4 = 1,00
    public async Task S4_KhongGiuViecNao_Bang1()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Khánh Rảnh");
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).DiemThanhPhan.KhoiLuong.Diem.Should().BeApproximately(1.00, 0.01);
    }

    [Fact] // tai = K -> S4 = 0,00
    public async Task S4_TaiBangNguongK_Bang0()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Lâm Đủ tải", maxConcurrentTasks: 8);
        ThemViecDangGiu(mt, u, 8);   // 8 viec THUONGXUYEN, trong so 1,0 => tai = 8 = K
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var uv = Tim(kq, u);

        uv.DiemThanhPhan.KhoiLuong.Diem.Should().BeApproximately(0.00, 0.01);
        uv.SoLieu.TaiTrongSo.Should().BeApproximately(8.0, 0.01);
        uv.SoLieu.K.Should().Be(8);
    }

    [Fact] // §9.4 S4 — do khan co trong so: DOTXUAT 2,0 · TRONGTAM 1,5 · THUONGXUYEN 1,0
    public async Task S4_TaiCoTrongSoTheoDoKhan()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Minh Việc gấp", maxConcurrentTasks: 8);
        ThemViecDangGiu(mt, u, 2, DoKhan.DotXuat);      // 2 x 2,0 = 4,0
        ThemViecDangGiu(mt, u, 2, DoKhan.TrongTam);     // 2 x 1,5 = 3,0
        ThemViecDangGiu(mt, u, 1, DoKhan.ThuongXuyen);  // 1 x 1,0 = 1,0
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        Tim(kq, u).SoLieu.TaiTrongSo.Should().BeApproximately(8.0, 0.01);
    }

    [Fact] // §9.3 dieu 6 — nguoi qua tai VAN co trong danh sach nhung bi day xuong CUOI
    public async Task S4_NguoiQuaTai_VanCoTrongDanhSachNhungXepCuoi()
    {
        await using var mt = await TaoMoiTruongAsync();
        var manh = ThemNguoiThucHien(mt, "Aa Mạnh và rảnh");
        var yeu = ThemNguoiThucHien(mt, "Bb Yếu nhưng rảnh");
        var quaTai = ThemNguoiThucHien(mt, "Cc Giỏi nhưng quá tải", maxConcurrentTasks: 8);

        ThemViecDaNghiemThu(mt, manh, LinhVucChinh, 5, 5);
        ThemViecDaNghiemThu(mt, yeu, LinhVucChinh, 1, 1);
        ThemViecDaNghiemThu(mt, quaTai, LinhVucChinh, 5, 5);
        ThemViecDangGiu(mt, quaTai, 12);   // tai = 12 >= 8 x 1,5 => "Qua tai"
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        kq.UngVien.Should().HaveCount(3);
        kq.UngVien.Select(x => x.UserId).Should().Contain(quaTai.Id, "§9.3 dieu 6 KHONG loai han nguoi qua tai");
        kq.UngVien[^1].UserId.Should().Be(quaTai.Id, "nguoi qua tai luon xep sau moi nguoi khong qua tai");
        kq.UngVien[^1].Nhan.Select(n => n.Ma).Should().Contain(MaNhanUngVien.QuaTai);
        kq.UngVien[0].UserId.Should().Be(manh.Id);
    }

    // =====================================================================
    // §9.4 S5 — Tinh san sang: S5 = 1 - min(1, q / 3)
    // =====================================================================

    [Theory] // q=0 -> 1,00 · q=1 -> 0,67 · q=2 -> 0,33 · q>=3 -> 0,00
    [InlineData(0, 1.00)]
    [InlineData(1, 0.67)]
    [InlineData(2, 0.33)]
    [InlineData(3, 0.00)]
    [InlineData(5, 0.00)]
    public async Task S5_SanSang_TinhTheoSoViecQuaHan(int soViecQuaHan, double mong)
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Nam Quá hạn", maxConcurrentTasks: 20);
        ThemViecDangGiu(mt, u, soViecQuaHan, DoKhan.ThuongXuyen, TrangThaiNv.DangTrienKhaiQuaHan);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var uv = Tim(kq, u);

        uv.DiemThanhPhan.SanSang.Diem.Should().BeApproximately(mong, 0.01);
        uv.SoLieu.SoNvQuaHan.Should().Be(soViecQuaHan);
    }

    // =====================================================================
    // §9.1 "giai thich duoc" — TONG 5 GOP PHAN PHAI BANG DIEM TONG
    // =====================================================================

    [Fact] // §9.1 + §9.6 — bang phan ra tren M06 cong lai phai ra dung diem tong
    public async Task TongNamGopPhan_BangDiemTong_TrenNhieuHoSoKhacNhau()
    {
        await using var mt = await TaoMoiTruongAsync();
        var hoSo = new (string Ten, int SoNghiemThu, int SoDungHan, int SoDangGiu, string LinhVuc)[]
        {
            ("Hồ sơ 01", 0, 0, 0, LinhVucChinh),
            ("Hồ sơ 02", 1, 1, 1, LinhVucChinh),
            ("Hồ sơ 03", 2, 1, 3, LinhVucChinh),
            ("Hồ sơ 04", 3, 2, 0, LinhVucAnhEm),
            ("Hồ sơ 05", 4, 4, 2, LinhVucChinh),
            ("Hồ sơ 06", 5, 3, 5, LinhVucKhacNhom),
            ("Hồ sơ 07", 6, 6, 8, LinhVucChinh),
            ("Hồ sơ 08", 8, 5, 4, LinhVucAnhEm),
            ("Hồ sơ 09", 10, 9, 0, LinhVucChinh),
            ("Hồ sơ 10", 12, 7, 6, LinhVucChinh),
            ("Hồ sơ 11", 15, 15, 12, LinhVucKhacNhom),
            ("Hồ sơ 12", 20, 11, 2, LinhVucAnhEm)
        };

        foreach (var h in hoSo)
        {
            var u = ThemNguoiThucHien(mt, h.Ten, maxConcurrentTasks: 8);
            ThemViecDaNghiemThu(mt, u, h.LinhVuc, h.SoNghiemThu, h.SoDungHan);
            ThemViecDangGiu(mt, u, h.SoDangGiu);
        }
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        kq.UngVien.Should().HaveCount(hoSo.Length);
        foreach (var uv in kq.UngVien)
        {
            var tong = uv.DiemThanhPhan.ChuyenMon.GopPhan
                     + uv.DiemThanhPhan.LichSu.GopPhan
                     + uv.DiemThanhPhan.HieuQua.GopPhan
                     + uv.DiemThanhPhan.KhoiLuong.GopPhan
                     + uv.DiemThanhPhan.SanSang.GopPhan;

            uv.DiemTong.Should().BeApproximately(tong, 0.05,
                "§9.1 giai thich duoc: 5 thanh phan cong lai phai bang diem tong (ứng viên {0})", uv.FullName);
            uv.DiemThanhPhan.TongGopPhan.Should().BeApproximately(uv.DiemTong, 0.05);
            uv.DiemTong.Should().BeInRange(0, 100);
            uv.DoTinCay.Should().BeInRange(0, 1);
            uv.LyDo.Should().NotBeEmpty("§9.1 bat buoc kem ly do tieng Viet");
            uv.LyDo.Count.Should().BeLessThanOrEqualTo(GioiHan.SoDongLyDoToiDa, "§9.7 toi da 4 dong ly do");
        }
    }

    // =====================================================================
    // §9.6 / §8 T9 — tinh bat bien
    // =====================================================================

    [Fact] // Goi hai lan lien tiep cho ket qua giong het nhau
    public async Task GoiY_GoiHaiLan_ChoKetQuaGiongNhau()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        for (var i = 1; i <= 6; i++)
        {
            var u = ThemNguoiThucHien(mt, $"Ứng viên {i:D2}");
            ThemViecDaNghiemThu(mt, u, LinhVucChinh, i, i - 1);
            ThemViecDangGiu(mt, u, i % 4);
        }
        await mt.LuuAsync();

        var lan1 = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var lan2 = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        lan2.UngVien.Select(x => x.UserId).Should().Equal(lan1.UngVien.Select(x => x.UserId));
        lan2.UngVien.Select(x => x.DiemTong).Should().Equal(lan1.UngVien.Select(x => x.DiemTong));
    }

    [Fact] // Doi thu tu du lieu dau vao KHONG lam doi ket qua (§8 T9)
    public async Task GoiY_DoiThuTuDauVao_KhongDoiKetQua()
    {
        var hoSo = new (string Ten, int SoNghiemThu, int SoDungHan, int SoDangGiu)[]
        {
            ("Ứng viên A", 5, 5, 1),
            ("Ứng viên B", 3, 1, 4),
            ("Ứng viên C", 8, 6, 2),
            ("Ứng viên D", 0, 0, 0),
            ("Ứng viên E", 2, 2, 7)
        };

        var xuoi = await ChayVoiThuTuAsync(hoSo);
        var nguoc = await ChayVoiThuTuAsync(hoSo.AsEnumerable().Reverse().ToArray());

        nguoc.Select(x => x.Ten).Should().Equal(xuoi.Select(x => x.Ten));
        nguoc.Select(x => x.Diem).Should().Equal(xuoi.Select(x => x.Diem));
    }

    private static async Task<List<(string Ten, double Diem)>> ChayVoiThuTuAsync(
        (string Ten, int SoNghiemThu, int SoDungHan, int SoDangGiu)[] hoSo)
    {
        await using var mt = await TaoMoiTruongAsync();
        foreach (var h in hoSo)
        {
            var u = ThemNguoiThucHien(mt, h.Ten);
            ThemViecDaNghiemThu(mt, u, LinhVucChinh, h.SoNghiemThu, h.SoDungHan);
            ThemViecDangGiu(mt, u, h.SoDangGiu);
        }
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        return kq.UngVien.Select(x => (x.FullName, x.DiemTong)).ToList();
    }

    [Fact] // §9.5 "Nguyen tac": khong bao gio tra danh sach rong
    public async Task GoiY_KhiMoiNguoiDeuChuaCoDuLieu_VanKhongTraDanhSachRong()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiThucHien(mt, "Người mới 1");
        ThemNguoiThucHien(mt, "Người mới 2");
        ThemNguoiThucHien(mt, "Người mới 3");
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        kq.UngVien.Should().NotBeEmpty();
        kq.CanhBao.Should().NotBeEmpty();
    }

    [Fact] // §9.6 — soLuong gioi han so ban ghi tra ve
    public async Task GoiY_TonTrongSoLuongYeuCau()
    {
        await using var mt = await TaoMoiTruongAsync();
        for (var i = 1; i <= 9; i++)
        {
            var u = ThemNguoiThucHien(mt, $"Ứng viên {i:D2}");
            ThemViecDaNghiemThu(mt, u, LinhVucChinh, i, i);
        }
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(soLuong: 5), CancellationToken.None);

        kq.UngVien.Should().HaveCount(5);
        kq.TongSoUngVien.Should().Be(9, "tongSoUngVien dem TRUOC khi cat theo soLuong");
    }

    // =====================================================================
    // §9.3 — Loc cung
    // =====================================================================

    [Fact] // §9.3 dieu 1 va 2 — loai tai khoan khoa va nguoi khong co vai NGUOI_THUC_HIEN
    public async Task LocCung_LoaiTaiKhoanKhoaVaNguoiKhongPhaiNguoiThucHien()
    {
        await using var mt = await TaoMoiTruongAsync();
        var hopLe = ThemNguoiThucHien(mt, "Hợp lệ");
        var biKhoa = Xuong.NguoiThucHien(trangThai: 0);
        biKhoa.FullName = "Bị khoá";
        var nguoiGiao = Xuong.NguoiGiao();
        var quanTri = Xuong.QuanTri();
        mt.Them(biKhoa, nguoiGiao, quanTri);
        ThemViecDaNghiemThu(mt, hopLe, LinhVucChinh, 3, 3);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var ids = kq.UngVien.Select(x => x.UserId).ToList();

        ids.Should().Contain(hopLe.Id);
        ids.Should().NotContain(biKhoa.Id);
        ids.Should().NotContain(nguoiGiao.Id);
        ids.Should().NotContain(quanTri.Id);
    }

    [Fact] // §9.3 dieu 3 — ngoai pham vi don vi duoc chon
    public async Task LocCung_LoaiNguoiNgoaiPhamViDonVi()
    {
        await using var mt = await TaoMoiTruongAsync();
        mt.Them(new SysUnit { UnitCode = "P09", TenDonVi = "Phòng khác", CapDonVi = 2, TrangThai = 1 });
        var trongPhamVi = ThemNguoiThucHien(mt, "Trong phạm vi");
        var ngoaiPhamVi = Xuong.NguoiDung(VaiTro.NguoiThucHien, unitCode: "P09", hoTen: "Ngoài phạm vi");
        mt.Them(ngoaiPhamVi);
        await mt.LuuAsync();

        var req = YeuCau();
        req.PhamViUnitCode.Add("P01");
        var kq = await mt.GoiY.GoiYAsync(req, CancellationToken.None);

        kq.UngVien.Select(x => x.UserId).Should().Contain(trongPhamVi.Id).And.NotContain(ngoaiPhamVi.Id);
    }

    [Fact] // §9.3 dieu 4 va 5 — da phan cong / da tung tu choi chinh nhiem vu nay
    public async Task LocCung_LoaiNguoiDaPhanCongVaDaTungTuChoi()
    {
        await using var mt = await TaoMoiTruongAsync();
        var conLai = ThemNguoiThucHien(mt, "Còn lại");
        var daPhanCong = ThemNguoiThucHien(mt, "Đã phân công");
        var daTuChoi = ThemNguoiThucHien(mt, "Đã từ chối");
        await mt.LuuAsync();

        var req = YeuCau();
        req.DaPhanCong.Add(daPhanCong.Id);
        req.DaTuChoi.Add(daTuChoi.Id);
        var kq = await mt.GoiY.GoiYAsync(req, CancellationToken.None);
        var ids = kq.UngVien.Select(x => x.UserId).ToList();

        ids.Should().Contain(conLai.Id);
        ids.Should().NotContain(daPhanCong.Id);
        ids.Should().NotContain(daTuChoi.Id);
        kq.DaLocTrungLap.Should().BeTrue();
        kq.LoaiBo.Select(x => x.UserId).Should().Contain(new[] { daPhanCong.Id, daTuChoi.Id });
    }

    [Fact] // §9.6 request.loaiTru — nguoi giao chu dong loai
    public async Task LocCung_TonTrongDanhSachLoaiTru()
    {
        await using var mt = await TaoMoiTruongAsync();
        var giu = ThemNguoiThucHien(mt, "Giữ lại");
        var loai = ThemNguoiThucHien(mt, "Loại trừ");
        await mt.LuuAsync();

        var req = YeuCau();
        req.LoaiTru.Add(loai.Id);
        var kq = await mt.GoiY.GoiYAsync(req, CancellationToken.None);

        kq.UngVien.Select(x => x.UserId).Should().Contain(giu.Id).And.NotContain(loai.Id);
    }

    // =====================================================================
    // §9.5 — Cold start / che do khoi tao
    // =====================================================================

    [Fact] // §9.5 dong cuoi — toan he thong chua co nhiem vu nghiem thu => che do KHOI_TAO
    public async Task CheDoKhoiTao_KhiHeThongChuaCoDuLieuNghiemThu()
    {
        await using var mt = await TaoMoiTruongAsync();
        var u = ThemNguoiThucHien(mt, "Người mới duy nhất");
        ThemViecDangGiu(mt, u, 2);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        kq.CheDo.Should().Be(CheDoGoiY.KhoiTao);
        kq.CanhBao.Should().NotBeEmpty();
        // §9.5: che do du phong chi xep theo S1 (0,6) va S4 (0,4)
        var uv = Tim(kq, u);
        uv.DiemThanhPhan.ChuyenMon.TrongSo.Should().BeApproximately(0.6, 0.001);
        uv.DiemThanhPhan.KhoiLuong.TrongSo.Should().BeApproximately(0.4, 0.001);
        uv.DiemThanhPhan.LichSu.TrongSo.Should().Be(0);
        uv.DiemThanhPhan.HieuQua.TrongSo.Should().Be(0);
        uv.DiemThanhPhan.SanSang.TrongSo.Should().Be(0);
    }

    [Fact] // §9.5 — he thong DA co du lieu nghiem thu => che do DAY_DU, dung trong so §9.2
    public async Task CheDoDayDu_DungTrongSoMacDinh()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        var u = ThemNguoiThucHien(mt, "Ứng viên thường");
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);
        var uv = Tim(kq, u);

        kq.CheDo.Should().Be(CheDoGoiY.DayDu);
        uv.DiemThanhPhan.ChuyenMon.TrongSo.Should().BeApproximately(0.30, 0.001);
        uv.DiemThanhPhan.LichSu.TrongSo.Should().BeApproximately(0.20, 0.001);
        uv.DiemThanhPhan.HieuQua.TrongSo.Should().BeApproximately(0.25, 0.001);
        uv.DiemThanhPhan.KhoiLuong.TrongSo.Should().BeApproximately(0.20, 0.001);
        uv.DiemThanhPhan.SanSang.TrongSo.Should().BeApproximately(0.05, 0.001);
    }

    [Fact] // §9.6 — hai khoa nay LUON null o v1 (§9.2 / §10.1 bo bang khai bao nang luc)
    public async Task SoLieu_MucThanhThaoVaDiemChatLuongTb_LuonNullOPhienBanV1()
    {
        await using var mt = await TaoMoiTruongAsync();
        ThemNguoiMoc(mt);
        await mt.LuuAsync();

        var kq = await mt.GoiY.GoiYAsync(YeuCau(), CancellationToken.None);

        kq.UngVien.Should().OnlyContain(x => x.SoLieu.MucThanhThao == null);
        kq.UngVien.Should().OnlyContain(x => x.SoLieu.DiemChatLuongTb == null);
        kq.PhienBanTrongSo.Should().NotBeNullOrWhiteSpace();
    }

    // =====================================================================
    // §5.8 H4 — kiem tra cau hinh trong so
    // =====================================================================

    [Fact] // §9.2 — w1 + ... + w5 = 1,00
    public async Task KiemTraCauHinh_TrongSoMacDinhHopLe()
    {
        await using var mt = await TaoMoiTruongAsync();

        var kt = mt.GoiY.KiemTraCauHinh(new CauHinhAiDto());

        kt.TongTrongSo.Should().BeApproximately(1.00, 0.0001);
        kt.HopLe.Should().BeTrue();
        kt.CanhBao.Should().BeEmpty();
    }

    [Fact] // §9.2 — tong trong so khac 1,00 phai bi canh bao
    public async Task KiemTraCauHinh_TongTrongSoSaiThiCanhBao()
    {
        await using var mt = await TaoMoiTruongAsync();

        var kt = mt.GoiY.KiemTraCauHinh(new CauHinhAiDto { W1 = 0.50 });

        kt.HopLe.Should().BeFalse();
        kt.CanhBao.Should().NotBeEmpty();
    }
}
