using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using QLNV.Core.Abstractions;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Seed;

/// <summary>
/// Mot ung vien trong <c>ket_qua_json</c> cua <c>AI_GOIY_LOG</c> (§4.8).
/// Khai o cap namespace (khong long trong lop) de System.Text.Json phan chieu duoc de dang.
/// </summary>
public sealed class UngVienLogMau
{
    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("diemTong")]
    public double DiemTong { get; set; }

    [JsonPropertyName("thuHang")]
    public int ThuHang { get; set; }
}

/// <summary>
/// Bo sinh du lieu mau — PORT tu <c>seed.js</c> (muc 9 "Bo sinh du lieu chinh").
///
/// TAT DINH: moi so ngau nhien deu lay tu <see cref="BoNgauNhien"/> co seed co dinh,
/// moi khoa chinh lay tu <see cref="MaGuid"/>. Chay lai LUON cho ra bo du lieu Y HET.
///
/// Bo du lieu bam cac yeu cau cua §8 T9 va §9:
///  - 5 don vi, 11 linh vuc (3 nhom cha + 8 la) de demo §9.4 S1 fallback nhom cha;
///  - 22 nguoi dung, du 8 CHAN DUNG (a)-(h) cho kich ban demo AI;
///  - 324 nhiem vu lich su da nghiem thu (truc A thuoc {1,5} VA truc B = 11) rai 18 thang;
///  - 28 nhiem vu dang song phu het cac o cua ma tran §2.4;
///  - 72 ban ghi AI_GOIY_LOG cho Precision@1 = 0,444 · Precision@3 = 0,694 ·
///    ty le chap nhan = 0,806 · MRR = 0,577 (dung khoang muc tieu §9.8).
/// </summary>
public static class BoSinhDuLieuMau
{
    /// <summary>Phut hop le khi gan "gio hanh chinh" cho mot moc thoi gian.</summary>
    private static readonly int[] PhutHanhChinh = { 0, 10, 15, 20, 30, 40, 45, 50 };

    /// <summary>So ngay thuc hien thuong gap cua mot nhiem vu.</summary>
    private static readonly int[] SoNgayThucHien = { 7, 10, 14, 15, 20, 21, 25, 30 };

    /// <summary>Diem nen cua tung thu hang trong nhat ky goi y (§9.8).</summary>
    private static readonly double[] DiemNen = { 86.4, 80.1, 74.6, 69.2, 63.5 };

    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    /// <summary>Mot "cho" nhiem vu lich su truoc khi duoc dung thanh entity.</summary>
    private sealed class OLichSu
    {
        public string MaNguoiDung = string.Empty;
        public string LinhVuc = string.Empty;
        public int TrangThai = TrangThaiNv.HoanThanh;
        public bool BiTraLai;
        public int SoLanGiaHan;
        public int ClMin;
        public int ClMax;
    }

    /// <summary>Thong ke tho theo nguoi dung — port ham <c>thongKe</c> cua seed.js.</summary>
    private sealed class ThongKeNguoiDung
    {
        public int Tong;
        public int DungHan;
        public readonly Dictionary<string, int> TheoLinhVuc = new(StringComparer.Ordinal);
        public int TraLai;
        public int GiaHan;
        public double Tai;
        public int DangMo;
        public int QuaHan;
        public int SoPhanCong;
    }

    /// <summary>
    /// Sinh toan bo bo du lieu mau.
    /// </summary>
    /// <param name="homNay">Ngay lam moc. Null =&gt; dung <see cref="DanhMucMau.NgayMocMacDinh"/>.</param>
    /// <param name="bamMatKhau">Bo bam mat khau — mat khau demo la <c>123456</c> cho moi tai khoan.</param>
    /// <param name="seed">Seed PRNG. Giu nguyen gia tri mac dinh de tai lap duoc bo du lieu.</param>
    public static DuLieuMau Tao(DateOnly? homNay, IPasswordHasher bamMatKhau, int seed = BoNgauNhien.SeedMacDinh)
    {
        ArgumentNullException.ThrowIfNull(bamMatKhau);

        var today = homNay ?? DanhMucMau.NgayMocMacDinh;
        var rnd = new BoNgauNhien(seed);
        var db = new DuLieuMau { HomNay = today };

        // =====================================================================
        // 1. Danh muc tinh: don vi, linh vuc, tu dien, nguoi dung
        // =====================================================================
        foreach (var dv in DanhMucMau.DonVi)
        {
            db.DonVi.Add(new SysUnit
            {
                UnitCode = dv.UnitCode,
                TenDonVi = dv.TenDonVi,
                MaCha = dv.MaCha,
                CapDonVi = dv.CapDonVi,
                TrangThai = 1
            });
        }

        int thuTuLv = 0;
        foreach (var lv in DanhMucMau.LinhVuc)
        {
            db.LinhVuc.Add(new DmLinhVuc
            {
                Ma = lv.Ma,
                Ten = lv.Ten,
                NhomCha = lv.NhomCha,
                TrangThai = 1,
                ThuTu = ++thuTuLv
            });
        }

        ThemTuDien(db);

        // Mat khau demo bam MOT LAN roi dung chung cho 22 tai khoan (BCrypt rat cham).
        // §10.11: chi luu chuoi bam, khong bao gio luu mat khau ro.
        string chuoiBamDemo = bamMatKhau.Bam(DanhMucMau.MatKhauDemo);
        var moc = today.ToDateTime(new TimeOnly(8, 0));

        var userTheoMa = new Dictionary<string, SysUser>(StringComparer.Ordinal);
        foreach (var u in DanhMucMau.NguoiDung)
        {
            var user = new SysUser
            {
                Id = MaGuid.Tu(u.Ma),
                UserName = u.UserName,
                PasswordHash = chuoiBamDemo,
                FullName = u.FullName,
                Email = u.UserName + "@sotttt.gov.vn",
                UnitCode = u.UnitCode,
                ChucVu = u.ChucVu,
                VaiTro = u.VaiTro,
                TrangThai = u.TrangThai,
                MaxConcurrentTasks = u.MaxConcurrentTasks,
                RefreshToken = null,
                RefreshTokenHetHan = null,
                CreateDate = moc.AddDays(-600)
            };
            db.NguoiDung.Add(user);
            userTheoMa[u.Ma] = user;
        }

        // =====================================================================
        // 2. Dung "cho" cho 324 nhiem vu lich su theo ho so tung nguoi (seed.js 9.1)
        // =====================================================================
        var oLichSu = new List<OLichSu>();
        foreach (var hs in DanhMucMau.HoSoLichSu)
        {
            var ds = new List<OLichSu>();
            foreach (var (maLv, soNv) in hs.LinhVuc)
            {
                for (int i = 0; i < soNv; i++)
                {
                    ds.Add(new OLichSu
                    {
                        MaNguoiDung = hs.Ma,
                        LinhVuc = maLv,
                        TrangThai = TrangThaiNv.HoanThanh,
                        BiTraLai = false,
                        SoLanGiaHan = 0,
                        ClMin = hs.ClMin,
                        ClMax = hs.ClMax
                    });
                }
            }

            int n = ds.Count;

            // Dung han / sau han: lay DUNG so luong theo ty le, khong phu thuoc may rui
            int soDungHan = LamTronJs(hs.DungHan * n);
            var hv1 = rnd.Tron(BoNgauNhien.DayChiSo(n));
            for (int i = 0; i < n; i++)
            {
                ds[hv1[i]].TrangThai = i < soDungHan ? TrangThaiNv.HoanThanh : TrangThaiNv.HoanThanhSauHan;
            }

            // §9.4 S3 (b): tung bi cham CHUA DAT
            var hv2 = rnd.Tron(BoNgauNhien.DayChiSo(n));
            for (int j = 0; j < hs.TraLai && j < n; j++) ds[hv2[j]].BiTraLai = true;

            // §9.4 S3 (c): so lan gia han
            var hv3 = rnd.Tron(BoNgauNhien.DayChiSo(n));
            int k = 0;
            for (int g2 = 0; g2 < hs.GiaHan2 && k < n; g2++) ds[hv3[k++]].SoLanGiaHan = 2;
            for (int g1 = 0; g1 < hs.GiaHan1 && k < n; g1++) ds[hv3[k++]].SoLanGiaHan = 1;

            oLichSu.AddRange(ds);
        }

        // Gom theo linh vuc — moi van ban chi dao chi chua nhiem vu CUNG linh vuc
        var theoLinhVuc = new Dictionary<string, List<OLichSu>>(StringComparer.Ordinal);
        foreach (var o in oLichSu)
        {
            if (!theoLinhVuc.TryGetValue(o.LinhVuc, out var ds))
            {
                ds = new List<OLichSu>();
                theoLinhVuc[o.LinhVuc] = ds;
            }
            ds.Add(o);
        }

        // Ung vien co the duoc rut lam nguoi PHOI HOP cua viec lich su.
        // Loai tru CHAN DUNG (d) — nguoi moi tinh phai KHONG co bat ky ban ghi nao.
        var dsThucHien = DanhMucMau.NguoiDung
            .Where(u => u.VaiTro == VaiTro.NguoiThucHien && u.Ma != DanhMucMau.ChanDung.DNguoiMoi)
            .ToList();

        // Giu cho truoc noi dung cua nhiem vu dang song de viec lich su khong trung
        var noiDungDaDung = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in MauNoiDung.NhiemVuSong) noiDungDaDung.Add(s.NoiDung);

        var soKyHieuDaDung = new HashSet<string>(StringComparer.Ordinal);
        foreach (var v in MauNoiDung.VanBanNoiBat) soKyHieuDaDung.Add(v.SoKyHieu);

        // Bo dem sinh ma ban ghi
        var dem = new int[6]; // 0: vb, 1: nv, 2: pc, 3: xl, 4: gh, 5: ai

        // =====================================================================
        // 3. Sinh van ban lich su + nhiem vu lich su
        // =====================================================================
        foreach (var lv in DanhMucMau.LinhVucLa)
        {
            var nguon = theoLinhVuc.TryGetValue(lv, out var ds0) ? ds0 : new List<OLichSu>();
            var ds = rnd.Tron(nguon);
            int i = 0;
            while (i < ds.Count)
            {
                int soCon = Math.Min(rnd.Nguyen(1, 4), ds.Count - i);
                var nhom = ds.GetRange(i, soCon);
                TaoVanBanLichSu(db, rnd, today, lv, nhom, userTheoMa, dsThucHien,
                    noiDungDaDung, soKyHieuDaDung, dem);
                i += soCon;
            }
        }

        // =====================================================================
        // 4. 12 van ban noi bat + 28 nhiem vu dang song
        // =====================================================================
        foreach (var v in MauNoiDung.VanBanNoiBat)
        {
            var ngayBh = today.AddDays(v.LechNgayBanHanh);
            db.VanBan.Add(new DmVanBan
            {
                Id = MaGuid.Tu(v.Ma),
                SoKyHieu = v.SoKyHieu,
                TrichYeu = v.TrichYeu,
                LoaiVb = v.LoaiVb,
                NgayBanHanh = ngayBh,
                CoQuanBanHanh = v.CoQuan,
                DoKhan = v.DoKhan,
                LinhVuc = v.LinhVuc,
                ThoiGianChiDao = ngayBh.ToDateTime(new TimeOnly(8, 0)),
                NguonNv = v.Nguon,
                NguoiTheoDoi = null,
                UnitCode = "DV00",
                UserIdCreate = userTheoMa["U02"].Id,
                CreateDate = ngayBh.ToDateTime(new TimeOnly(8, 0)),
                UpdateDate = null
            });
        }

        var idNvTheoMaSong = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var s in MauNoiDung.NhiemVuSong)
        {
            TaoNhiemVuSong(db, rnd, today, s, userTheoMa, dem, idNvTheoMaSong);
        }

        // =====================================================================
        // 5. AI_GOIY_LOG — 72 lan goi y da xay ra (§4.8, §9.8)
        // =====================================================================
        TaoNhatKyAi(db, rnd, userTheoMa, idNvTheoMaSong, dem);

        // =====================================================================
        // 6. Chuan hoa moc thoi gian
        // =====================================================================
        ChuanHoaMocXuLy(db);
        ChuanHoaUpdateDate(db);

        return db;
    }

    // =========================================================================
    // Danh muc DM_TUDIEN (§7.4 muc 3 — app moi BAT BUOC tu seed bang nay)
    // =========================================================================
    private static void ThemTuDien(DuLieuMau db)
    {
        var trangThaiNv = new (int Ma, string Mau)[]
        {
            (TrangThaiNv.HoanThanh, "xanh"),
            (TrangThaiNv.DangTrienKhai, "duong"),
            (TrangThaiNv.ChuaTrienKhai, "xam"),
            (TrangThaiNv.HoanThanhSauHan, "cam"),
            (TrangThaiNv.TuChoi, "do"),
            (TrangThaiNv.DangTrienKhaiQuaHan, "do"),
            (TrangThaiNv.GiaHan, "tim"),
            (TrangThaiNv.DaThuHoi, "xam")
        };

        int thuTu = 0;
        foreach (var (ma, mau) in trangThaiNv)
        {
            db.TuDien.Add(TaoTuDien(MaTypeTuDien.TrangThaiNv, ma.ToString(CultureInfo.InvariantCulture),
                TrangThaiNv.Nhan(ma), mau, ++thuTu, null));
        }

        var trangThaiPh = new (int Ma, string Mau)[]
        {
            (TrangThaiPh.ChoXacNhan, "vang"),
            (TrangThaiPh.DaXacNhan, "xanh"),
            (TrangThaiPh.TuChoi, "do")
        };

        thuTu = 0;
        foreach (var (ma, mau) in trangThaiPh)
        {
            db.TuDien.Add(TaoTuDien(MaTypeTuDien.TrangThaiPh, ma.ToString(CultureInfo.InvariantCulture),
                TrangThaiPh.Nhan(ma), mau, ++thuTu, null));
        }

        thuTu = 0;
        foreach (var (ma, nhan) in DanhMucMau.NhanLoaiVb)
        {
            db.TuDien.Add(TaoTuDien(MaTypeTuDien.LoaiVb, ma, nhan, null, ++thuTu, null));
        }

        var doKhan = new (string Ma, string Mau)[]
        {
            (DoKhan.DotXuat, "do"),
            (DoKhan.TrongTam, "cam"),
            (DoKhan.ThuongXuyen, "xam")
        };

        thuTu = 0;
        foreach (var (ma, mau) in doKhan)
        {
            db.TuDien.Add(TaoTuDien(MaTypeTuDien.DoKhan, ma, DoKhan.Nhan(ma), mau, ++thuTu,
                "Trọng số tải dùng cho AI: " + DoKhan.TrongSo(ma).ToString("0.0", CultureInfo.InvariantCulture)));
        }
    }

    private static DmTuDien TaoTuDien(string type, string ma, string nhan, string? mau, int thuTu, string? moTa) =>
        new()
        {
            Id = MaGuid.Tu("TD:" + type + ":" + ma),
            Type = type,
            Ma = ma,
            Nhan = nhan,
            Mau = mau,
            MoTa = moTa,
            ThuTu = thuTu,
            TrangThai = 1
        };

    // =========================================================================
    // Van ban lich su + cac nhiem vu con cua no
    // =========================================================================
    private static void TaoVanBanLichSu(
        DuLieuMau db,
        BoNgauNhien rnd,
        DateOnly today,
        string lv,
        List<OLichSu> dsO,
        Dictionary<string, SysUser> userTheoMa,
        List<NguoiDungMau> dsThucHien,
        HashSet<string> noiDungDaDung,
        HashSet<string> soKyHieuDaDung,
        int[] dem)
    {
        // Ngay ban hanh rai trong ~18 thang gan nhat, du xa de moi moc deu <= today
        var ngayBh = today.AddDays(-rnd.Nguyen(75, 545));
        string loai = rnd.Chon(DanhMucMau.LoaiVb);
        string coQuan = rnd.Chon(DanhMucMau.CoQuanBanHanh);

        string soHieu;
        if (coQuan == "Ủy ban nhân dân tỉnh")
        {
            loai = "CONGVAN";
            soHieu = rnd.Nguyen(800, 4200).ToString(CultureInfo.InvariantCulture) + "/UBND-KGVX";
        }
        else if (coQuan == "Bộ Thông tin và Truyền thông")
        {
            loai = "CONGVAN";
            soHieu = rnd.Nguyen(500, 3000).ToString(CultureInfo.InvariantCulture) + "/BTTTT-CNTT";
        }
        else
        {
            soHieu = rnd.Nguyen(20, 480).ToString(CultureInfo.InvariantCulture) + "/" + DanhMucMau.HauToSoKyHieu[loai];
        }

        // Tranh trung so ky hieu: tang dan phan so dung truoc dau '/'
        int viTri = soHieu.IndexOf('/');
        string hauTo = soHieu.Substring(viTri);
        int soDau = int.Parse(soHieu.Substring(0, viTri), CultureInfo.InvariantCulture);
        while (soKyHieuDaDung.Contains(soHieu))
        {
            soDau++;
            soHieu = soDau.ToString(CultureInfo.InvariantCulture) + hauTo;
        }
        soKyHieuDaDung.Add(soHieu);

        string maVb = MaGuid.Ma("VBH", ++dem[0], 3);
        string trichYeu = rnd.Chon(MauNoiDung.TrichYeuTheoLinhVuc[lv]);
        string doKhanVb = rnd.Chon(DanhMucMau.DoKhanTheoTanSuat);
        string nguonNv = rnd.Chon(DanhMucMau.NguonNhiemVu);
        var mocVb = ngayBh.ToDateTime(new TimeOnly(8, 0));

        var idVb = MaGuid.Tu(maVb);
        db.VanBan.Add(new DmVanBan
        {
            Id = idVb,
            SoKyHieu = soHieu,
            TrichYeu = trichYeu,
            LoaiVb = loai,
            NgayBanHanh = ngayBh,
            CoQuanBanHanh = coQuan,
            DoKhan = doKhanVb,
            LinhVuc = lv,
            ThoiGianChiDao = mocVb,
            NguonNv = nguonNv,
            NguoiTheoDoi = null,
            UnitCode = "DV00",
            UserIdCreate = userTheoMa["U02"].Id,
            CreateDate = mocVb,
            UpdateDate = null
        });

        foreach (var o in dsO)
        {
            TaoNhiemVuLichSu(db, rnd, today, idVb, ngayBh, o, userTheoMa, dsThucHien, noiDungDaDung, dem);
        }
    }

    private static void TaoNhiemVuLichSu(
        DuLieuMau db,
        BoNgauNhien rnd,
        DateOnly today,
        Guid idVb,
        DateOnly ngayBh,
        OLichSu o,
        Dictionary<string, SysUser> userTheoMa,
        List<NguoiDungMau> dsThucHien,
        HashSet<string> noiDungDaDung,
        int[] dem)
    {
        var u = userTheoMa[o.MaNguoiDung];
        var nguoiGiao = userTheoMa[MaNguoiGiaoCua(u.UnitCode)];

        string doKhan = rnd.Chon(DanhMucMau.DoKhanTheoTanSuat);
        var ngayGiao = ngayBh.AddDays(rnd.Nguyen(0, 2));
        int soNgayGoc = rnd.Chon(SoNgayThucHien);
        var hanGoc = ngayGiao.AddDays(soNgayGoc);

        // Moi lan gia han da duyet day han xu ly ra xa
        var han = hanGoc;
        var buoc = new List<(DateOnly Cu, DateOnly Moi)>();
        for (int g = 0; g < o.SoLanGiaHan; g++)
        {
            int them = rnd.Nguyen(5, 8);
            var moi = han.AddDays(them);
            buoc.Add((han, moi));
            han = moi;
        }

        // §1.2 buoc 3: tiep nhan SAU khi giao, lech toi thieu 1 ngay
        var tiepNhan = ngayGiao.AddDays(rnd.Nguyen(1, 2));

        DateOnly hoanThanh = o.TrangThai == TrangThaiNv.HoanThanh
            ? han.AddDays(-rnd.Nguyen(1, Math.Max(1, soNgayGoc / 3)))
            : han.AddDays(rnd.Nguyen(1, 12));
        if (hoanThanh.DayNumber - tiepNhan.DayNumber < 4) hoanThanh = tiepNhan.AddDays(4);

        // §1.2 buoc 5-6: nghiem thu LUON dien ra SAU khi gui bao cao
        var ngayNghiemThu = hoanThanh.AddDays(rnd.Nguyen(1, 3));
        if (ngayNghiemThu.DayNumber > today.DayNumber) ngayNghiemThu = today;

        string maNv = MaGuid.Ma("NV", ++dem[1], 4);
        var idNv = MaGuid.Tu(maNv);
        bool coChatLuong = rnd.Kha(0.5);
        var gioGiao = GioLamViec(ngayGiao, rnd);

        // Thu tu goi PRNG duoi day bam dung thu tu thuoc tinh cua seed.js
        string noiDung = SinhNoiDung(rnd, o.LinhVuc, noiDungDaDung);
        var mocTiepNhan = GioLamViec(tiepNhan, rnd);
        var mocHoanThanh = GioLamViec(hoanThanh, rnd);
        string phanHoi = rnd.Chon(MauNoiDung.YKienDat);
        int? hsChatLuong = coChatLuong ? rnd.Nguyen(o.ClMin, o.ClMax) : null;
        var mocCapNhat = GioLamViec(ngayNghiemThu, rnd);

        db.NhiemVu.Add(new DmNhiemVuChiTiet
        {
            Id = idNv,
            IdVb = idVb,
            NoiDung = noiDung,
            LinhVuc = o.LinhVuc,
            DoKhan = doKhan,
            HanXuLyTh = han,
            SoNgayHxlTh = han.DayNumber - ngayGiao.DayNumber,
            HanXuLyPh = null,
            NgayGiao = gioGiao,
            NgayTiepNhan = mocTiepNhan,
            NgayHoanThanhThucTe = mocHoanThanh,
            TrangThai = o.TrangThai,
            TrangThaiDvXuly = TrangThaiPh.DaXacNhan,
            TrangThaiXuLyGiaHan = o.SoLanGiaHan > 0 ? TrangThaiGiaHan.DaDuyet : null,
            SoLanGiaHan = o.SoLanGiaHan,
            MucDoHt = 100,
            PhanHoi = phanHoi,
            HsChatLuong = hsChatLuong,
            UserIdGiaoViec = nguoiGiao.Id,
            UserIdCreate = nguoiGiao.Id,
            UnitCode = u.UnitCode,
            CreateDate = gioGiao,
            UpdateDate = mocCapNhat,
            AiGoiYId = null
        });

        ThemPhanCong(db, dem, idNv, u, VaiTroPhanCong.ChuTri, nguoiGiao.Id, gioGiao,
            TrangThaiPhanCong.ConHieuLuc);

        // ~20% viec co them nguoi phoi hop (khac nguoi chu tri — §4.3)
        if (rnd.Kha(0.2))
        {
            var ph = rnd.Chon(dsThucHien);
            if (ph.Ma != o.MaNguoiDung)
            {
                ThemPhanCong(db, dem, idNv, userTheoMa[ph.Ma], VaiTroPhanCong.PhoiHop, nguoiGiao.Id,
                    gioGiao, TrangThaiPhanCong.ConHieuLuc);
            }
        }

        // Cac de xuat gia han DA DUYET
        foreach (var (cu, moi) in buoc)
        {
            var mocTao = GioLamViec(cu.AddDays(-2), rnd);
            var mocDuyet = GioLamViec(cu.AddDays(-1), rnd);
            db.GiaHan.Add(new GiaHanNhiemVu
            {
                Id = MaGuid.Tu(MaGuid.Ma("GH", ++dem[4], 4)),
                IdCtnv = idNv,
                NoiDung = "Khối lượng công việc phát sinh, đề nghị gia hạn thời gian hoàn thành.",
                HanXuLyDeXuat = moi,
                HanXuLyThCu = cu,
                TrangThai = TrangThaiGiaHan.DaDuyet,
                PhanHoi = "Đồng ý gia hạn.",
                UserIdDeXuat = u.Id,
                UserIdDuyet = nguoiGiao.Id,
                CreateDate = mocTao,
                NgayDuyet = mocDuyet,
                // BO SUNG: truc A ngay truoc khi chuyen sang 13 (§2.4 T11)
                TrangThaiCu = TrangThaiNv.DangTrienKhai
            });
        }

        // --- Chuoi xu ly: TIEPNHAN -> TIENDO... -> [BAOCAO bi tra lai] -> BAOCAO -> NGHIEMTHU
        int mocDau = tiepNhan.DayNumber;
        int mocCuoi = hoanThanh.DayNumber;
        int nhip = mocCuoi - mocDau;

        ThemXuLy(db, dem, idNv, LoaiXuLy.TiepNhan, MauNoiDung.NoiDungTiepNhan, 0,
            TrangThaiNv.DangTrienKhai, null, null, u.Id, GioLamViec(tiepNhan, rnd));

        int soTienDo = rnd.Nguyen(1, 3);
        for (int t = 1; t <= soTienDo; t++)
        {
            var ngayTd = DateOnly.FromDayNumber(mocDau + LamTronJs((double)nhip * t / (soTienDo + 2)));
            string noiDungTd = rnd.Chon(MauNoiDung.NoiDungTienDo);
            int phanTram = LamTronJs(90.0 * t / (soTienDo + 1));
            ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, noiDungTd, phanTram,
                null, null, null, u.Id, GioLamViec(ngayTd, rnd));
        }

        if (o.BiTraLai)
        {
            // §9.4 S3: ban ghi bao cao BI CHAM CHUA DAT — trangthaiDvXuly = 12.
            // Ghi ngay tren chinh ban ghi BAOCAO cua nguoi thuc hien de dem duoc
            // ca theo useridXuly lan theo nhiem vu ma ho chu tri.
            var ngayTraLai = DateOnly.FromDayNumber(mocDau + LamTronJs(nhip * 0.7));
            string noiDungBc = rnd.Chon(MauNoiDung.NoiDungBaoCao);
            ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao, noiDungBc, 100, o.TrangThai,
                TrangThaiPh.ChoXacNhan, TrangThaiPh.TuChoi, u.Id, GioLamViec(ngayTraLai, rnd));

            var ngayBoSung = DateOnly.FromDayNumber(mocDau + LamTronJs(nhip * 0.85));
            ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo,
                "Đã bổ sung, chỉnh sửa theo ý kiến của lãnh đạo.", 95,
                null, null, null, u.Id, GioLamViec(ngayBoSung, rnd));
        }

        string noiDungBaoCao = rnd.Chon(MauNoiDung.NoiDungBaoCao);
        ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao, noiDungBaoCao, 100, o.TrangThai,
            TrangThaiPh.ChoXacNhan, null, u.Id, GioLamViec(hoanThanh, rnd));

        string yKien = rnd.Chon(MauNoiDung.YKienDat);
        ThemXuLy(db, dem, idNv, LoaiXuLy.NghiemThu, yKien, 100, o.TrangThai,
            null, TrangThaiPh.DaXacNhan, nguoiGiao.Id, GioLamViec(ngayNghiemThu, rnd));
    }

    // =========================================================================
    // 28 nhiem vu dang song (§2.4 ma tran T1-T14)
    // =========================================================================
    private static void TaoNhiemVuSong(
        DuLieuMau db,
        BoNgauNhien rnd,
        DateOnly today,
        NhiemVuSongMau s,
        Dictionary<string, SysUser> userTheoMa,
        int[] dem,
        Dictionary<string, Guid> idNvTheoMaSong)
    {
        var u = userTheoMa[s.ChuTri];
        var nguoiGiao = userTheoMa[MaNguoiGiaoCua(u.UnitCode)];
        var ngayGiao = today.AddDays(s.LechGiao);
        var han = ngayGiao.AddDays(s.SoNgay);

        DateOnly? tiepNhan =
            s.KichBan == KichBanSong.ChuaTiepNhan || s.KichBan == KichBanSong.TuChoi
                ? null
                : ngayGiao.AddDays(1);

        string maNv = MaGuid.Ma("NV", ++dem[1], 4);
        var idNv = MaGuid.Tu(maNv);
        idNvTheoMaSong[s.Ma] = idNv;
        var gioGiao = GioLamViec(ngayGiao, rnd);

        // Ngay hoan thanh thuc te — chi voi cac kich ban da gui bao cao ket qua
        DateOnly? hoanThanh = null;
        if (s.KichBan == KichBanSong.ChoXacNhan || s.KichBan == KichBanSong.DaNghiemThu)
        {
            hoanThanh = DateOnly.FromDayNumber(Math.Min(han.DayNumber - 1, today.DayNumber - 1));
        }
        else if (s.KichBan == KichBanSong.ChoXacNhanSauHan)
        {
            hoanThanh = DateOnly.FromDayNumber(Math.Min(han.DayNumber + rnd.Nguyen(1, 5), today.DayNumber - 1));
        }

        string? phanHoi = null;
        int? hsChatLuong = null;
        if (s.KichBan == KichBanSong.BiTraLai) phanHoi = rnd.Chon(MauNoiDung.LyDoTraLai);
        if (s.KichBan == KichBanSong.DaNghiemThu)
        {
            phanHoi = rnd.Chon(MauNoiDung.YKienDat);
            hsChatLuong = 5;
        }
        if (s.KichBan == KichBanSong.DaThuHoi)
        {
            phanHoi = "Thu hồi nhiệm vụ do điều chỉnh phân công giữa các phòng.";
        }

        var mocTiepNhan = tiepNhan.HasValue ? GioLamViec(tiepNhan.Value, rnd) : (DateTime?)null;
        var mocHoanThanh = hoanThanh.HasValue ? GioLamViec(hoanThanh.Value, rnd) : (DateTime?)null;
        var ngayCapNhat = hoanThanh ?? tiepNhan ?? ngayGiao;
        var mocCapNhat = GioLamViec(ngayCapNhat, rnd);

        db.NhiemVu.Add(new DmNhiemVuChiTiet
        {
            Id = idNv,
            IdVb = MaGuid.Tu(s.Vb),
            NoiDung = s.NoiDung,
            LinhVuc = s.LinhVuc,
            DoKhan = s.DoKhan,
            HanXuLyTh = han,
            SoNgayHxlTh = s.SoNgay,
            HanXuLyPh = null,
            NgayGiao = gioGiao,
            NgayTiepNhan = mocTiepNhan,
            NgayHoanThanhThucTe = mocHoanThanh,
            TrangThai = s.TrangThai,
            TrangThaiDvXuly = s.TrangThaiDvXuly,
            TrangThaiXuLyGiaHan = s.TrangThaiXuLyGiaHan,
            SoLanGiaHan = 0,
            MucDoHt = s.MucDoHt,
            PhanHoi = phanHoi,
            HsChatLuong = hsChatLuong,
            UserIdGiaoViec = nguoiGiao.Id,
            UserIdCreate = nguoiGiao.Id,
            UnitCode = u.UnitCode,
            CreateDate = gioGiao,
            UpdateDate = mocCapNhat,
            AiGoiYId = null
        });

        // §4.3: giu vet phan cong CHUTRI cu da bi thu hoi (trangthai = 0) truoc khi giao lai
        if (!string.IsNullOrEmpty(s.ChuTriCu) && s.ChuTriCu != s.ChuTri)
        {
            ThemPhanCong(db, dem, idNv, userTheoMa[s.ChuTriCu], VaiTroPhanCong.ChuTri, nguoiGiao.Id,
                gioGiao, TrangThaiPhanCong.DaThuHoi);
        }

        ThemPhanCong(db, dem, idNv, u, VaiTroPhanCong.ChuTri, nguoiGiao.Id, gioGiao,
            TrangThaiPhanCong.ConHieuLuc);

        foreach (var maPh in s.PhoiHop)
        {
            if (maPh == s.ChuTri) continue;
            ThemPhanCong(db, dem, idNv, userTheoMa[maPh], VaiTroPhanCong.PhoiHop, nguoiGiao.Id,
                gioGiao, TrangThaiPhanCong.ConHieuLuc);
        }

        if (tiepNhan.HasValue)
        {
            ThemXuLy(db, dem, idNv, LoaiXuLy.TiepNhan, MauNoiDung.NoiDungTiepNhan, 0,
                TrangThaiNv.DangTrienKhai, null, null, u.Id, GioLamViec(tiepNhan.Value, rnd));
        }

        DateOnly? giuaKy = tiepNhan.HasValue
            ? DateOnly.FromDayNumber(LamTronJs((tiepNhan.Value.DayNumber + (hoanThanh ?? today).DayNumber) / 2.0))
            : null;

        switch (s.KichBan)
        {
            case KichBanSong.ChuaTiepNhan:
                break;

            case KichBanSong.DangLam:
            case KichBanSong.QuaHan:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, Math.Max(5, LamTronJs(s.MucDoHt / 2.0)),
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));
                string nd2 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd2, s.MucDoHt,
                    null, null, null, u.Id, GioLamViec(today.AddDays(-1), rnd));
                break;
            }

            case KichBanSong.ChoXacNhan:
            case KichBanSong.ChoXacNhanSauHan:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, 60,
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));
                string nd2 = rnd.Chon(MauNoiDung.NoiDungBaoCao);
                ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao, nd2, 100, s.TrangThai,
                    TrangThaiPh.ChoXacNhan, null, u.Id, GioLamViec(hoanThanh!.Value, rnd));
                break;
            }

            case KichBanSong.BiTraLai:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, 55,
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));

                // Bao cao da bi cham CHUA DAT (§2.2 ma 12) -> day nhiem vu ve (2, 12).
                // Chi ghi MOT ban ghi mang trangthaiDvXuly = 12 de §9.4 S3 khong dem lap.
                string nd2 = rnd.Chon(MauNoiDung.NoiDungBaoCao);
                ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao,
                    nd2 + " [Người giao đánh giá CHƯA ĐẠT: " + phanHoi + "]",
                    100, TrangThaiNv.HoanThanh, TrangThaiPh.ChoXacNhan, TrangThaiPh.TuChoi,
                    u.Id, GioLamViec(today.AddDays(-6), rnd));

                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo,
                    "Đang bổ sung, hoàn thiện theo ý kiến phản hồi của lãnh đạo.", s.MucDoHt,
                    null, null, null, u.Id, GioLamViec(today.AddDays(-2), rnd));
                break;
            }

            case KichBanSong.TuChoi:
                ThemXuLy(db, dem, idNv, LoaiXuLy.TuChoi,
                    "Nội dung nhiệm vụ không thuộc chức năng, nhiệm vụ của phòng; đề nghị chuyển đơn vị khác thực hiện.",
                    0, TrangThaiNv.TuChoi, TrangThaiPh.ChoXacNhan, TrangThaiPh.ChoXacNhan,
                    u.Id, GioLamViec(ngayGiao.AddDays(1), rnd));
                break;

            case KichBanSong.XinGiaHan:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, s.MucDoHt,
                    null, null, null, u.Id, GioLamViec(today.AddDays(-3), rnd));

                db.GiaHan.Add(new GiaHanNhiemVu
                {
                    Id = MaGuid.Tu(MaGuid.Ma("GH", ++dem[4], 4)),
                    IdCtnv = idNv,
                    NoiDung = "Khối lượng khảo sát thực địa lớn hơn dự kiến, đề nghị gia hạn thêm 15 ngày để hoàn thành.",
                    HanXuLyDeXuat = han.AddDays(15),
                    HanXuLyThCu = han,
                    TrangThai = TrangThaiGiaHan.ChoDuyet,
                    PhanHoi = null,
                    UserIdDeXuat = u.Id,
                    UserIdDuyet = null,
                    CreateDate = GioLamViec(today.AddDays(-1), rnd),
                    NgayDuyet = null,
                    // Truoc khi xin gia han, nhiem vu dang o (2, null) — con han
                    TrangThaiCu = TrangThaiNv.DangTrienKhai
                });
                break;
            }

            case KichBanSong.DaNghiemThu:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, 70,
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));
                string nd2 = rnd.Chon(MauNoiDung.NoiDungBaoCao);
                ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao, nd2, 100, TrangThaiNv.HoanThanh,
                    TrangThaiPh.ChoXacNhan, null, u.Id, GioLamViec(hoanThanh!.Value, rnd));
                ThemXuLy(db, dem, idNv, LoaiXuLy.NghiemThu, phanHoi, 100, TrangThaiNv.HoanThanh,
                    null, TrangThaiPh.DaXacNhan, nguoiGiao.Id, GioLamViec(today.AddDays(-2), rnd));
                break;
            }

            case KichBanSong.DaThuHoi:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, s.MucDoHt,
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));
                break;
            }

            case KichBanSong.GiaHanBiTuChoi:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, s.MucDoHt,
                    null, null, null, u.Id, GioLamViec(today.AddDays(-8), rnd));

                // §2.4 T13 + §2.3 ma 12: nguoi giao TU CHOI gia han ->
                // trangthaixulygiahan := 12, solangiahan giu nguyen, nhiem vu tro lai (7, null).
                db.GiaHan.Add(new GiaHanNhiemVu
                {
                    Id = MaGuid.Tu(MaGuid.Ma("GH", ++dem[4], 4)),
                    IdCtnv = idNv,
                    NoiDung = "Phải chờ số liệu đối chiếu của các đơn vị trực thuộc, đề nghị gia hạn thêm 10 ngày.",
                    HanXuLyDeXuat = han.AddDays(10),
                    HanXuLyThCu = han,
                    TrangThai = TrangThaiGiaHan.TuChoi,
                    PhanHoi = "Nhiệm vụ đã quá hạn nhiều ngày, đề nghị tập trung hoàn thành ngay. Không đồng ý gia hạn.",
                    UserIdDeXuat = u.Id,
                    UserIdDuyet = nguoiGiao.Id,
                    CreateDate = GioLamViec(today.AddDays(-5), rnd),
                    NgayDuyet = GioLamViec(today.AddDays(-4), rnd),
                    // Da qua han truoc khi xin gia han -> khoi phuc ve 7 (§2.5)
                    TrangThaiCu = TrangThaiNv.DangTrienKhaiQuaHan
                });
                break;
            }

            case KichBanSong.ThuHoiBaoCao:
            {
                string nd1 = rnd.Chon(MauNoiDung.NoiDungTienDo);
                ThemXuLy(db, dem, idNv, LoaiXuLy.TienDo, nd1, 60,
                    null, null, null, u.Id, GioLamViec(giuaKy!.Value, rnd));
                string nd2 = rnd.Chon(MauNoiDung.NoiDungBaoCao);
                ThemXuLy(db, dem, idNv, LoaiXuLy.BaoCao, nd2, 100, TrangThaiNv.HoanThanh,
                    TrangThaiPh.ChoXacNhan, null, u.Id, GioLamViec(today.AddDays(-4), rnd));

                // §2.4 T8: nguoi thuc hien thu hoi bao cao dang cho xac nhan -> ve (2, null)
                ThemXuLy(db, dem, idNv, LoaiXuLy.ThuHoiBaoCao,
                    "Phát hiện thiếu số liệu của 02 đơn vị, xin thu hồi báo cáo để bổ sung trước khi trình lãnh đạo.",
                    s.MucDoHt, TrangThaiNv.DangTrienKhai, null, null, u.Id, GioLamViec(today.AddDays(-2), rnd));
                break;
            }
        }
    }

    // =========================================================================
    // AI_GOIY_LOG (§4.8, §9.8)
    // =========================================================================
    private static void TaoNhatKyAi(
        DuLieuMau db,
        BoNgauNhien rnd,
        Dictionary<string, SysUser> userTheoMa,
        Dictionary<string, Guid> idNvTheoMaSong,
        int[] dem)
    {
        var tk = ThongKe(db);

        // Ma noi bo theo Guid — de xep hang co tie-break on dinh
        var maTheoId = new Dictionary<Guid, string>();
        foreach (var kv in userTheoMa) maTheoId[kv.Value.Id] = kv.Key;

        var dsUngVien = DanhMucMau.NguoiDung
            .Where(u => u.VaiTro == VaiTro.NguoiThucHien && u.TrangThai == 1)
            .Select(u => userTheoMa[u.Ma])
            .ToList();

        // Nguoi chu tri con hieu luc dau tien cua tung nhiem vu
        var chuTriTheoNv = new Dictionary<Guid, Guid>();
        foreach (var pc in db.PhanCong)
        {
            if (pc.VaiTro != VaiTroPhanCong.ChuTri) continue;
            if (pc.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            if (chuTriTheoNv.ContainsKey(pc.IdNvChiTiet)) continue;
            chuTriTheoNv[pc.IdNvChiTiet] = pc.UserId;
        }

        var nguoiDungTheoId = db.NguoiDung.ToDictionary(x => x.Id);
        var idSong = new HashSet<Guid>(idNvTheoMaSong.Values);

        // 32 lan chon hang 1, 10 hang 2, 8 hang 3, 5 hang 4, 3 hang 5, 14 lan bo qua
        var ketCucGoc = new List<int?>();
        var bang = new (int? Hang, int SoLan)[] { (1, 32), (2, 10), (3, 8), (4, 5), (5, 3), (null, 14) };
        foreach (var (hang, soLan) in bang)
        {
            for (int i = 0; i < soLan; i++) ketCucGoc.Add(hang);
        }
        var ketCuc = rnd.Tron(ketCucGoc);

        // 60 viec lich su gan day nhat (chu tri KHONG bi khoa) + 12 viec dang song
        var dsLichSuGanDay = db.NhiemVu
            .Where(nv =>
                nv.TrangThaiDvXuly == TrangThaiPh.DaXacNhan
                && !idSong.Contains(nv.Id)
                && chuTriTheoNv.TryGetValue(nv.Id, out var ct)
                && nguoiDungTheoId.TryGetValue(ct, out var u)
                && u.TrangThai == 1)
            .OrderByDescending(nv => nv.NgayGiao)
            .ThenBy(nv => nv.Id)
            .Take(60)
            .ToList();

        var nvTheoId = db.NhiemVu.ToDictionary(x => x.Id);
        var dsNvCoLog = new List<DmNhiemVuChiTiet>(dsLichSuGanDay);
        foreach (var ma in MauNoiDung.NhiemVuSongCoLogAi)
        {
            if (idNvTheoMaSong.TryGetValue(ma, out var id) && nvTheoId.TryGetValue(id, out var nv))
            {
                dsNvCoLog.Add(nv);
            }
        }

        for (int i = 0; i < dsNvCoLog.Count; i++)
        {
            var nv = dsNvCoLog[i];
            if (!chuTriTheoNv.TryGetValue(nv.Id, out var chuTri)) continue;

            int? hang = ketCuc[i % ketCuc.Count];
            var top = XepHangUngVien(dsUngVien, tk, maTheoId, nv.LinhVuc, chuTri);

            if (hang.HasValue)
            {
                if (top.Count > 4) top = top.GetRange(0, 4);
                int viTri = Math.Min(hang.Value - 1, top.Count);
                top.Insert(viTri, chuTri);
            }

            var ketQua = new List<UngVienLogMau>(top.Count);
            for (int k = 0; k < top.Count; k++)
            {
                double diemNen = k < DiemNen.Length ? DiemNen[k] : DiemNen[^1];
                // Lam tron 1 chu so thap phan, dung quy tac lam tron cua JavaScript
                double diem = LamTronJs((diemNen + (rnd.So() * 3 - 1.5)) * 10) / 10.0;
                ketQua.Add(new UngVienLogMau { UserId = top[k], DiemTong = diem, ThuHang = k + 1 });
            }

            var idLog = MaGuid.Tu(MaGuid.Ma("AI", ++dem[5], 3));
            db.AiLog.Add(new AiGoiYLog
            {
                Id = idLog,
                IdNvChiTiet = nv.Id,
                LinhVuc = nv.LinhVuc,
                KetQuaJson = JsonSerializer.Serialize(ketQua, TuyChonJson),
                UserIdDaChon = hang.HasValue ? chuTri : null,
                ThuHangDaChon = hang,
                // §4.8 — CHOT CUNG top-5, khong phu thuoc soLuong cua request
                CoTrongGoiY = hang.HasValue,
                PhienBanTrongSo = i < dsNvCoLog.Count - 20 ? "v1.0" : "v1.1",
                CreateDate = nv.NgayGiao,
                UserIdGoiY = nv.UserIdGiaoViec,
                CheDo = CheDoGoiY.DayDu,
                SoUngVien = ketQua.Count
            });

            if (hang.HasValue) nv.AiGoiYId = idLog;
        }
    }

    /// <summary>
    /// Xep hang ung vien tho de sinh <c>ket_qua_json</c> cua nhat ky mau.
    /// KHONG phai cong thuc §9.4 — day chi la mot ham xap xi de bo du lieu trong hop ly
    /// (chuyen mon dung linh vuc an nhat, roi den tong so viec, tru di tai hien tai).
    /// </summary>
    private static List<Guid> XepHangUngVien(
        List<SysUser> dsUngVien,
        Dictionary<Guid, ThongKeNguoiDung> tk,
        Dictionary<Guid, string> maTheoId,
        string? linhVuc,
        Guid loaiTru)
    {
        var diem = new List<(Guid Id, double D, string Ma)>();
        foreach (var u in dsUngVien)
        {
            if (u.Id == loaiTru) continue;
            if (!tk.TryGetValue(u.Id, out var o)) continue;
            int theoLv = linhVuc is not null && o.TheoLinhVuc.TryGetValue(linhVuc, out var c) ? c : 0;
            diem.Add((u.Id, theoLv * 3 + o.Tong * 0.3 - o.Tai * 0.5, maTheoId[u.Id]));
        }

        return diem
            .OrderByDescending(x => x.D)
            .ThenBy(x => x.Ma, StringComparer.Ordinal)
            .Take(5)
            .Select(x => x.Id)
            .ToList();
    }

    /// <summary>
    /// Thong ke tho theo nguoi dung — port ham <c>thongKe</c> cua seed.js.
    /// Chi dem tren cac nhiem vu nguoi do giu vai CHUTRI con hieu luc (§4.8 view).
    /// </summary>
    private static Dictionary<Guid, ThongKeNguoiDung> ThongKe(DuLieuMau db)
    {
        var kq = new Dictionary<Guid, ThongKeNguoiDung>();
        foreach (var u in db.NguoiDung) kq[u.Id] = new ThongKeNguoiDung();

        var nvTheoId = db.NhiemVu.ToDictionary(x => x.Id);

        foreach (var pc in db.PhanCong)
        {
            if (!kq.TryGetValue(pc.UserId, out var o)) continue;
            o.SoPhanCong++;

            if (pc.VaiTro != VaiTroPhanCong.ChuTri) continue;
            if (pc.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            if (!nvTheoId.TryGetValue(pc.IdNvChiTiet, out var nv)) continue;

            // §9.4 S1/S2: chi dem viec DA NGHIEM THU
            bool xong = (nv.TrangThai == TrangThaiNv.HoanThanh || nv.TrangThai == TrangThaiNv.HoanThanhSauHan)
                        && nv.TrangThaiDvXuly == TrangThaiPh.DaXacNhan;
            if (xong)
            {
                o.Tong++;
                if (nv.TrangThai == TrangThaiNv.HoanThanh) o.DungHan++;
                if (!string.IsNullOrEmpty(nv.LinhVuc))
                {
                    o.TheoLinhVuc[nv.LinhVuc] = o.TheoLinhVuc.TryGetValue(nv.LinhVuc, out var c) ? c + 1 : 1;
                }
                o.GiaHan += nv.SoLanGiaHan;
            }

            // §9.4 S4: tai hien tai (loai bo diem cuoi 1, 5, 97)
            if (nv.TrangThai != TrangThaiNv.HoanThanh
                && nv.TrangThai != TrangThaiNv.HoanThanhSauHan
                && nv.TrangThai != TrangThaiNv.DaThuHoi)
            {
                o.DangMo++;
                o.Tai += DoKhan.TrongSo(nv.DoKhan);
                if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan) o.QuaHan++; // §9.4 S5
            }
        }

        // §9.4 S3 (b): so lan bi tra lai
        foreach (var x in db.XuLy)
        {
            if (x.TrangThaiDvXuly == TrangThaiPh.TuChoi && kq.TryGetValue(x.UserIdXuLy, out var o)) o.TraLai++;
        }

        return kq;
    }

    // =========================================================================
    // Chuan hoa moc thoi gian
    // =========================================================================

    /// <summary>
    /// §3 M07/M09 — tab "Lich su xu ly" sap theo <c>ngayxuly</c>. Cac ban ghi duoc sinh
    /// theo dung thu tu nhan qua, nhung hai ban ghi CUNG NGAY co the boc phai gio dao nhau;
    /// day gio len de moc thoi gian cua moi nhiem vu luon TANG DAN.
    /// </summary>
    private static void ChuanHoaMocXuLy(DuLieuMau db)
    {
        var truoc = new Dictionary<Guid, DateTime>();
        foreach (var x in db.XuLy)
        {
            if (truoc.TryGetValue(x.IdCtnv, out var moc) && x.NgayXuLy <= moc)
            {
                x.NgayXuLy = moc.AddMinutes(15);
            }
            truoc[x.IdCtnv] = x.NgayXuLy;
        }
    }

    /// <summary>
    /// §4.2 — <c>updatedate</c> la moc CAP NHAT CUOI CUNG. Luoi M02/M08 sap theo cot nay
    /// nen no phai &gt;= thoi diem cua ban ghi xu ly / gia han muon nhat cua chinh nhiem vu do.
    /// </summary>
    private static void ChuanHoaUpdateDate(DuLieuMau db)
    {
        var mocCuoi = new Dictionary<Guid, DateTime>();

        void Ghi(Guid idNv, DateTime moc)
        {
            if (!mocCuoi.TryGetValue(idNv, out var cu) || moc > cu) mocCuoi[idNv] = moc;
        }

        foreach (var x in db.XuLy) Ghi(x.IdCtnv, x.NgayXuLy);
        foreach (var h in db.GiaHan) Ghi(h.IdCtnv, h.NgayDuyet ?? h.CreateDate);

        foreach (var nv in db.NhiemVu)
        {
            if (mocCuoi.TryGetValue(nv.Id, out var m) && nv.UpdateDate < m) nv.UpdateDate = m;
        }
    }

    // =========================================================================
    // Tien ich
    // =========================================================================

    /// <summary>Nguoi giao phu trach theo don vi cua nguoi thuc hien.</summary>
    private static string MaNguoiGiaoCua(string unitCode) => unitCode switch
    {
        "P01" => "U03",
        "P03" => "U04",
        _ => "U02"
    };

    /// <summary>Gan gio hanh chinh ngau nhien cho mot ngay.</summary>
    private static DateTime GioLamViec(DateOnly ngay, BoNgauNhien rnd)
    {
        int gio = rnd.Nguyen(8, 16);
        int phut = rnd.Chon(PhutHanhChinh);
        return ngay.ToDateTime(new TimeOnly(gio, phut));
    }

    /// <summary>
    /// Lam tron nua len (0,5 -&gt; 1) giong <c>Math.round</c> cua JavaScript.
    /// <see cref="Math.Round(double)"/> cua .NET lam tron ve so chan, cho ket qua KHAC.
    /// </summary>
    private static int LamTronJs(double x) => (int)Math.Floor(x + 0.5);

    /// <summary>Sinh noi dung nhiem vu khong trung lap (seed.js <c>sinhNoiDung</c>).</summary>
    private static string SinhNoiDung(BoNgauNhien rnd, string linhVuc, HashSet<string> daDung)
    {
        string mau = rnd.Chon(MauNoiDung.TheoLinhVuc[linhVuc]);
        string duoi = rnd.Chon(MauNoiDung.DuoiBoSung);
        string s = mau.Replace("{p}", duoi, StringComparison.Ordinal);

        if (daDung.Contains(s))
        {
            int k = 2;
            while (daDung.Contains(s + " (lần " + k.ToString(CultureInfo.InvariantCulture) + ")")) k++;
            s = s + " (lần " + k.ToString(CultureInfo.InvariantCulture) + ")";
        }

        daDung.Add(s);
        return s;
    }

    private static void ThemPhanCong(
        DuLieuMau db, int[] dem, Guid idNv, SysUser nguoiDung, string vaiTro,
        Guid nguoiTao, DateTime moc, int trangThai)
    {
        db.PhanCong.Add(new NhiemVuPhanCong
        {
            Id = MaGuid.Tu(MaGuid.Ma("PC", ++dem[2], 4)),
            IdNvChiTiet = idNv,
            UserId = nguoiDung.Id,
            UnitCode = nguoiDung.UnitCode,
            VaiTro = vaiTro,
            UserIdCreate = nguoiTao,
            TrangThai = trangThai,
            CreateDate = moc
        });
    }

    private static void ThemXuLy(
        DuLieuMau db, int[] dem, Guid idNv, string loai, string? noiDung, int? mucDoHt,
        int? trangThai, int? trangThaiXuLy, int? trangThaiDvXuly, Guid nguoiXuLy, DateTime moc)
    {
        db.XuLy.Add(new XuLyNhiemVu
        {
            Id = MaGuid.Tu(MaGuid.Ma("XL", ++dem[3], 5)),
            IdCtnv = idNv,
            Loai = loai,
            NoiDung = noiDung,
            MucDoHt = mucDoHt,
            TrangThai = trangThai,
            TrangThaiXuLy = trangThaiXuLy,
            TrangThaiDvXuly = trangThaiDvXuly,
            UserIdXuLy = nguoiXuLy,
            NgayXuLy = moc
        });
    }
}
