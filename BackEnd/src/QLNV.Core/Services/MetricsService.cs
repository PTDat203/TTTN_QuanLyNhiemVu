using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>§4.8 view <c>USER_TAI_HIENTAI</c> — tai hien tai cua mot nguoi thuc hien.</summary>
public sealed class TaiNguoiDung
{
    /// <summary>So nhiem vu dang giu (trangthai NOT IN 1, 5, 97).</summary>
    public int SoNvDangMo { get; set; }

    /// <summary>Tong trong so do khan cua cac nhiem vu dang giu.</summary>
    public double TaiTrongSo { get; set; }

    /// <summary>So nhiem vu dang o trangthai = 7.</summary>
    public int SoNvQuaHan { get; set; }
}

/// <summary>§9.8 dong "Do lech tai (Gini) — truoc va sau khi dung AI". Ky vong GIAM.</summary>
public sealed class GiniTruocSauDto
{
    /// <summary>Moc bat dau dung AI (ngay <c>createdate</c> som nhat trong AI_GOIY_LOG).</summary>
    public DateOnly? Moc { get; set; }

    /// <summary>Gini tai do tren anh chup CSDL tai moc.</summary>
    public double Truoc { get; set; }

    /// <summary>Gini tai hien tai.</summary>
    public double Sau { get; set; }

    /// <summary>Truoc - Sau. Duong la tot (tai deu hon).</summary>
    public double Giam { get; set; }

    /// <summary>Dat ky vong khi <see cref="Sau"/> &lt; <see cref="Truoc"/>.</summary>
    public bool Dat { get; set; }
}

/// <summary>§9.8 dong "Ty le dung han": nhom giao theo goi y AI so voi nhom giao thu cong.</summary>
public sealed class DungHanNhomDto
{
    public string Nhan { get; set; } = string.Empty;

    public int SoHoanThanh { get; set; }

    public int SoDungHan { get; set; }

    public double TyLeDungHan { get; set; }
}

/// <summary>§9.8 — ket qua so sanh ty le dung han giua hai nhom.</summary>
public sealed class DungHanTheoNguonDto
{
    public DungHanNhomDto TheoGoiY { get; set; } = new();

    public DungHanNhomDto ThuCong { get; set; } = new();

    /// <summary>Chenh lech ty le (AI - thu cong).</summary>
    public double ChenhLech { get; set; }

    /// <summary>Chi ket luan khi CA HAI nhom deu co mau.</summary>
    public bool Dat { get; set; }
}

/// <summary>
/// §9.8 — DO HIEU QUA AI + §3.1 M02 / §5.10 J1-J2 — THONG KE DASHBOARD.
///
/// Toan bo phep tinh nam trong ham STATIC THUAN TUY chay tren <see cref="AiSnapshot"/>,
/// khong cham CSDL, khong dung <c>DateTime.Now</c> (moc thoi gian lay tu
/// <see cref="AiSnapshot.HomNay"/>) — nho vay kiem thu duoc va tai lap duoc.
///
/// Quy uoc da kiem chung, PHAI GIU:
///  - mau so cua Precision@1 / Precision@3 / ty le chap nhan / MRR la TONG SO LAN GOI Y;
///    lan bo qua goi y dong gop 0 vao MRR nhung VAN nam trong mau so;
///  - so voi nguong muc tieu bang GIA TRI THO, khong dung gia tri da lam tron;
///  - <see cref="SoSanhBaseline"/> ton trong THU TU THOI GIAN: moi nhiem vu chi duoc
///    cham bang du lieu co TRUOC ngay giao cua chinh no (chan ro ri du lieu tuong lai).
/// </summary>
public sealed class MetricsService : IMetricsService
{
    // --- §2.1 mau cho bieu do tron M02 (nhan lay tu TrangThaiNv.Nhan de khong lech §10.5) ---
    private static readonly (int Ma, string Mau)[] BangMauTrangThai =
    {
        (TrangThaiNv.ChuaTrienKhai, "#94a3b8"),
        (TrangThaiNv.DangTrienKhai, "#2563eb"),
        (TrangThaiNv.DangTrienKhaiQuaHan, "#dc2626"),
        (TrangThaiNv.GiaHan, "#7c3aed"),
        (TrangThaiNv.TuChoi, "#f97316"),
        (TrangThaiNv.HoanThanh, "#16a34a"),
        (TrangThaiNv.HoanThanhSauHan, "#84cc16"),
        (TrangThaiNv.DaThuHoi, "#64748b")
    };

    // --- Khoang diem cho bieu do phan bo diem goi y (M13) ---
    private static readonly (string Khoang, double Min, double Max)[] KhoangDiem =
    {
        ("80-100", 80, double.MaxValue),
        ("60-79", 60, 80),
        ("40-59", 40, 60),
        ("20-39", 20, 40),
        ("0-19", double.MinValue, 20)
    };

    /// <summary>§9.8 — so nhiem vu lay mau khi so sanh baseline (du de chay nhanh duoi 2 giay).</summary>
    public const int SoMauBaselineMacDinh = 60;

    /// <summary>Seed co dinh cua baseline "chon ngau nhien" — KHONG dung so ngau nhien that (§9.9).</summary>
    private const uint SeedBaseline = 20260905u;

    private readonly IAiDataSource _nguon;

    public MetricsService(IAiDataSource nguon)
    {
        _nguon = nguon ?? throw new ArgumentNullException(nameof(nguon));
    }

    // ======================================================================
    // 1. GINI — §9.8 "Do lech tai". Cang thap cang deu.
    // ======================================================================

    /// <summary>
    /// He so Gini cua mot day gia tri khong am, ket qua thuoc [0, 1].
    /// <c>G = 2·Σ(i·x_i) / (n·Σx) − (n+1)/n</c>, i dem tu 1 tren day TANG DAN.
    /// n &lt; 2 hoac Σx = 0 =&gt; tra 0 (coi nhu tuyet doi deu) — chan chia 0.
    /// Lam tron 3 chu so: 2 chu so qua tho de phan biet cac chien luoc baseline.
    /// </summary>
    public static double Gini(IEnumerable<double> giaTri)
    {
        if (giaTri is null) return 0;

        var ds = new List<double>();
        foreach (var v in giaTri)
        {
            var x = v;
            if (double.IsNaN(x) || double.IsInfinity(x) || x < 0) x = 0;
            ds.Add(x);
        }

        var n = ds.Count;
        if (n < 2) return 0;

        var tong = 0.0;
        foreach (var v in ds) tong += v;
        if (tong <= 0) return 0;

        ds.Sort();
        var luyThua = 0.0;
        for (var i = 0; i < n; i++) luyThua += (i + 1) * ds[i];

        var g = 2 * luyThua / (n * tong) - (n + 1.0) / n;
        return SoHoc.LamTron(SoHoc.Keo(g, 0, 1), 3);
    }

    /// <summary>
    /// §4.8 view <c>USER_TAI_HIENTAI</c> — tai hien tai cua tung nguoi thuc hien.
    /// Moi ung vien hop le deu co mot dong (ke ca khi tai = 0) de Gini khong bi lech.
    /// </summary>
    public static Dictionary<Guid, TaiNguoiDung> TaiHienTai(AiSnapshot db)
    {
        ArgumentNullException.ThrowIfNull(db);
        var kq = new Dictionary<Guid, TaiNguoiDung>();

        foreach (var u in db.NguoiDung)
        {
            if (RecommendationService.LaUngVienHopLe(u)) kq[u.Id] = new TaiNguoiDung();
        }

        var nvTheoId = new Dictionary<Guid, DmNhiemVuChiTiet>();
        foreach (var nv in db.NhiemVu)
        {
            if (nv is not null) nvTheoId[nv.Id] = nv;
        }

        foreach (var p in db.PhanCong)
        {
            if (p is null) continue;
            if (!string.Equals(p.VaiTro, VaiTroPhanCong.ChuTri, StringComparison.Ordinal)) continue;
            if (p.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            if (!nvTheoId.TryGetValue(p.IdNvChiTiet, out var nv)) continue;
            if (!RecommendationService.DangGiuViec(nv)) continue;

            if (!kq.TryGetValue(p.UserId, out var dong))
            {
                dong = new TaiNguoiDung();
                kq[p.UserId] = dong;
            }
            dong.SoNvDangMo++;
            dong.TaiTrongSo += CauHinhAi.TrongSoDoKhan(nv.DoKhan);
            if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan) dong.SoNvQuaHan++;
        }

        foreach (var dong in kq.Values) dong.TaiTrongSo = SoHoc.LamTron(dong.TaiTrongSo, 2);
        return kq;
    }

    /// <summary>§9.8 — Gini cua phan bo <c>so_nv_dang_mo</c> giua cac nguoi thuc hien.</summary>
    public static double GiniTai(AiSnapshot db)
    {
        var bang = TaiHienTai(db);
        var ds = new List<double>(bang.Count);
        foreach (var dong in bang.Values) ds.Add(dong.SoNvDangMo);
        return Gini(ds);
    }

    // ======================================================================
    // 2. §9.8 — THONG KE HIEU QUA AI (tinh tu AI_GOIY_LOG)
    // ======================================================================

    /// <summary>
    /// §9.8 — Precision@1, Precision@3, ty le chap nhan, MRR + hai bieu do cua M13.
    /// </summary>
    public static ThongKeAiDto ThongKeAi(AiSnapshot db)
    {
        ArgumentNullException.ThrowIfNull(db);
        var logs = db.NhatKyAi;
        var tong = logs.Count;

        var soHang1 = 0;
        var soTop3 = 0;
        var soChapNhan = 0;
        var soBoQua = 0;
        var tongNghichDao = 0.0;
        var hangLonNhat = CauHinhAi.TopChotNhatKy;
        var demHang = new Dictionary<int, int>();

        var demKhoang = new Dictionary<string, int>();
        foreach (var k in KhoangDiem) demKhoang[k.Khoang] = 0;

        foreach (var log in logs)
        {
            if (log is null) continue;

            // §9.8 "Ty le chap nhan" = so lan chon tu danh sach goi y / tong so lan mo popup
            if (log.CoTrongGoiY) soChapNhan++;

            var hang = log.ThuHangDaChon.HasValue && log.ThuHangDaChon.Value >= 1
                ? log.ThuHangDaChon.Value
                : (int?)null;

            if (hang is null)
            {
                soBoQua++;                      // lan bo qua dong gop 0 vao MRR, VAN nam trong mau so
            }
            else
            {
                if (hang.Value == 1) soHang1++;             // Precision@1
                if (hang.Value <= 3) soTop3++;              // Precision@3
                tongNghichDao += 1.0 / hang.Value;          // MRR
                demHang[hang.Value] = (demHang.TryGetValue(hang.Value, out var d) ? d : 0) + 1;
                if (hang.Value > hangLonNhat) hangLonNhat = hang.Value;
            }

            // Phan bo diem: gom diem tong cua MOI ung vien tung duoc goi y
            foreach (var uv in RecommendationService.DocKetQuaJson(log.KetQuaJson))
            {
                foreach (var k in KhoangDiem)
                {
                    if (uv.DiemTong >= k.Min && uv.DiemTong < k.Max)
                    {
                        demKhoang[k.Khoang] = demKhoang[k.Khoang] + 1;
                        break;
                    }
                }
            }
        }

        var precision1 = SoHoc.ChiaAnToan(soHang1, tong);
        var precision3 = SoHoc.ChiaAnToan(soTop3, tong);
        var tyLeChapNhan = SoHoc.ChiaAnToan(soChapNhan, tong);
        var mrr = SoHoc.ChiaAnToan(tongNghichDao, tong);

        var phanBoThuHang = new List<PhanBoThuHangDto>();
        for (var i = 1; i <= hangLonNhat; i++)
        {
            phanBoThuHang.Add(new PhanBoThuHangDto
            {
                ThuHang = i,
                Nhan = "Hạng " + i,
                SoLan = demHang.TryGetValue(i, out var sl) ? sl : 0
            });
        }
        phanBoThuHang.Add(new PhanBoThuHangDto { ThuHang = null, Nhan = "Bỏ qua gợi ý", SoLan = soBoQua });

        var phanBoDiem = new List<PhanBoDiemDto>();
        foreach (var k in KhoangDiem)
        {
            phanBoDiem.Add(new PhanBoDiemDto { Khoang = k.Khoang, SoLan = demKhoang[k.Khoang] });
        }

        var nguong = new NguongMucTieuDto();
        return new ThongKeAiDto
        {
            SoLanGoiY = tong,
            SoLanChapNhan = soChapNhan,
            SoLanBoQua = soBoQua,
            TyLeChapNhan = SoHoc.LamTron(tyLeChapNhan, 2),
            Precision1 = SoHoc.LamTron(precision1, 2),
            Precision3 = SoHoc.LamTron(precision3, 2),
            Mrr = SoHoc.LamTron(mrr, 2),
            GiniTai = GiniTai(db),
            PhanBoThuHang = phanBoThuHang,
            PhanBoDiem = phanBoDiem,
            Nguong = nguong,
            // So bang GIA TRI THO, khong dung gia tri da lam tron
            Dat = new DatNguongDto
            {
                Precision1 = tong > 0 && precision1 >= nguong.Precision1,
                Precision3 = tong > 0 && precision3 >= nguong.Precision3,
                TyLeChapNhan = tong > 0 && tyLeChapNhan >= nguong.TyLeChapNhan,
                Mrr = tong > 0 && mrr >= nguong.Mrr
            }
        };
    }

    /// <summary>
    /// §9.8 dong "Ty le dung han": nhom nhiem vu giao THEO GOI Y (<c>ai_goiy_id</c> khac null)
    /// so voi nhom giao THU CONG. Chi tinh tren nhiem vu da nghiem thu;
    /// §9.4 S3(a): trangthai = 1 la dung han, trangthai = 5 la sau han.
    /// </summary>
    public static DungHanTheoNguonDto TyLeDungHanTheoNguon(AiSnapshot db)
    {
        ArgumentNullException.ThrowIfNull(db);
        var theoGoiY = new DungHanNhomDto { Nhan = "Giao theo gợi ý AI" };
        var thuCong = new DungHanNhomDto { Nhan = "Giao thủ công" };

        foreach (var nv in db.NhiemVu)
        {
            if (!RecommendationService.DaNghiemThu(nv)) continue;
            var nhom = nv!.AiGoiYId.HasValue && nv.AiGoiYId.Value != Guid.Empty ? theoGoiY : thuCong;
            nhom.SoHoanThanh++;
            if (nv.TrangThai == TrangThaiNv.HoanThanh) nhom.SoDungHan++;
        }

        var tlAi = SoHoc.ChiaAnToan(theoGoiY.SoDungHan, theoGoiY.SoHoanThanh);
        var tlTc = SoHoc.ChiaAnToan(thuCong.SoDungHan, thuCong.SoHoanThanh);
        theoGoiY.TyLeDungHan = SoHoc.LamTron(tlAi, 2);
        thuCong.TyLeDungHan = SoHoc.LamTron(tlTc, 2);

        return new DungHanTheoNguonDto
        {
            TheoGoiY = theoGoiY,
            ThuCong = thuCong,
            ChenhLech = SoHoc.LamTron(tlAi - tlTc, 2),
            Dat = theoGoiY.SoHoanThanh > 0 && thuCong.SoHoanThanh > 0 && tlAi > tlTc
        };
    }

    /// <summary>
    /// §9.8 dong "Do lech tai (Gini) — truoc va sau khi dung AI".
    /// "Truoc" do tren anh chup CSDL tai moc bat dau dung AI, dung lai dung bo cat
    /// chong ro ri cua <see cref="SoSanhBaseline"/>.
    /// </summary>
    public static GiniTruocSauDto GiniTaiTruocSau(AiSnapshot db, DateOnly? mocBatDauAi = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        var moc = mocBatDauAi;
        if (moc is null)
        {
            foreach (var log in db.NhatKyAi)
            {
                if (log is null) continue;
                var ngay = DateOnly.FromDateTime(log.CreateDate);
                if (moc is null || ngay < moc.Value) moc = ngay;
            }
        }

        var sau = GiniTai(db);
        if (moc is null)
        {
            // Chua tung dung AI => khong co moc de chia truoc/sau
            return new GiniTruocSauDto { Moc = null, Truoc = sau, Sau = sau, Giam = 0, Dat = false };
        }

        var anh = CatTheoMoc(db, moc.Value, null);
        var truoc = GiniTai(anh);
        return new GiniTruocSauDto
        {
            Moc = moc,
            Truoc = truoc,
            Sau = sau,
            Giam = SoHoc.LamTron(truoc - sau, 3),
            Dat = sau < truoc
        };
    }

    // ======================================================================
    // 3. §3.1 M02 / §5.10 J1-J2 — DASHBOARD
    // ======================================================================

    /// <summary>
    /// §5.10 J1 — 4 the dem + bieu do tron theo trang thai + bieu do cot theo don vi.
    ///
    /// QUY TAC DA KIEM CHUNG: chi nhiem vu DANG MO moi duoc dem vao "Qua han" /
    /// "Sap het han". §2.5 chi chuyen 2 -&gt; 7 va 3 -&gt; 7, nen viec da bao cao xong dang cho
    /// xac nhan (1|5, 10) hay da tu choi (6, *) KHONG duoc dem — neu dem thi the "Qua han"
    /// chong lan the "Cho xac nhan" cua chinh man M02.
    /// </summary>
    public static DashboardTongQuanDto TongQuan(AiSnapshot db, DateOnly homNay, int soNgaySapHetHan = GioiHan.NguongSapHetHan)
    {
        ArgumentNullException.ThrowIfNull(db);

        var kq = new DashboardTongQuanDto { HomNay = homNay };
        var demTrangThai = new Dictionary<int, int>();
        var theoDv = new Dictionary<string, DashboardDonViDto>(StringComparer.OrdinalIgnoreCase);
        var tongSo = 0;

        foreach (var nv in db.NhiemVu)
        {
            if (nv is null) continue;
            tongSo++;

            demTrangThai[nv.TrangThai] = (demTrangThai.TryGetValue(nv.TrangThai, out var d) ? d : 0) + 1;

            if (nv.TrangThai == TrangThaiNv.ChuaTrienKhai) kq.ChuaTrienKhai++;
            // 13 = dang cho duyet gia han, van la viec dang lam
            if (nv.TrangThai == TrangThaiNv.DangTrienKhai || nv.TrangThai == TrangThaiNv.GiaHan) kq.DangTrienKhai++;
            if (nv.TrangThaiDvXuly == TrangThaiPh.ChoXacNhan) kq.ChoXacNhan++;

            var daNghiemThu = RecommendationService.DaNghiemThu(nv);
            if (daNghiemThu) kq.HoanThanh++;

            var conLai = NgayUtil.SoNgayConLai(nv.HanXuLyTh, homNay);
            var quaHanNv = false;
            var daKetThuc = RecommendationService.DaKetThuc(nv);
            var laHoanThanh = nv.TrangThai == TrangThaiNv.HoanThanh || nv.TrangThai == TrangThaiNv.HoanThanhSauHan;

            if (!daKetThuc && !laHoanThanh && nv.TrangThai != TrangThaiNv.TuChoi)
            {
                if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan || (conLai.HasValue && conLai.Value < 0))
                {
                    quaHanNv = true;
                    kq.QuaHan++;
                }
                else if (conLai.HasValue && conLai.Value >= 0 && conLai.Value <= soNgaySapHetHan)
                {
                    kq.SapHetHan++;
                }
            }

            var maDv = string.IsNullOrWhiteSpace(nv.UnitCode) ? "(chưa rõ)" : nv.UnitCode;
            if (!theoDv.TryGetValue(maDv, out var dongDv))
            {
                dongDv = new DashboardDonViDto { UnitCode = maDv, TenDonVi = maDv };
                theoDv[maDv] = dongDv;
            }
            dongDv.TongSo++;
            if (daNghiemThu) dongDv.HoanThanh++;
            if (quaHanNv) dongDv.QuaHan++;
        }

        kq.TongSo = tongSo;
        kq.TyLeHoanThanh = SoHoc.LamTron(SoHoc.ChiaAnToan(kq.HoanThanh, tongSo), 2);

        // Bieu do tron — giu du 8 ma cua §2.1 de mau on dinh giua cac lan tai trang
        foreach (var m in BangMauTrangThai)
        {
            var soLuong = demTrangThai.TryGetValue(m.Ma, out var sl) ? sl : 0;
            kq.TheoTrangThai.Add(new DemTheoTrangThaiDto
            {
                Ma = m.Ma,
                Nhan = TrangThaiNv.Nhan(m.Ma),
                Mau = m.Mau,
                SoLuong = soLuong,
                TyLe = SoHoc.LamTron(SoHoc.ChiaAnToan(soLuong, tongSo), 2)
            });
        }

        // Ten don vi + ty le hoan thanh
        var tenDonVi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dv in db.DonVi)
        {
            if (dv is not null && !string.IsNullOrWhiteSpace(dv.UnitCode)) tenDonVi[dv.UnitCode] = dv.TenDonVi;
        }
        foreach (var dong in theoDv.Values)
        {
            if (tenDonVi.TryGetValue(dong.UnitCode, out var ten)) dong.TenDonVi = ten;
            dong.TyLeHoanThanh = SoHoc.LamTron(SoHoc.ChiaAnToan(dong.HoanThanh, dong.TongSo), 2);
            kq.TheoDonVi.Add(dong);
        }
        kq.TheoDonVi.Sort((a, b) =>
        {
            var soSanh = b.TongSo.CompareTo(a.TongSo);
            return soSanh != 0 ? soSanh : string.CompareOrdinal(a.TenDonVi, b.TenDonVi);
        });

        return kq;
    }

    /// <summary>§5.10 J2 — so nhiem vu va ty le hoan thanh theo don vi.</summary>
    public static IReadOnlyList<DashboardDonViDto> TheoDonVi(AiSnapshot db, DateOnly homNay) =>
        TongQuan(db, homNay).TheoDonVi;

    /// <summary>
    /// §3.1 M02 — "Viec cua toi sap den han (&lt;= 3 ngay)", ke ca viec da qua han.
    /// Chi lay nhiem vu ma nguoi dung dang co phan cong CON HIEU LUC va chua ket thuc.
    /// </summary>
    public static IReadOnlyList<NhiemVuSapDenHanDto> SapDenHan(
        AiSnapshot db, Guid userId, DateOnly homNay, int soNgay = GioiHan.NguongSapHetHan)
    {
        ArgumentNullException.ThrowIfNull(db);
        var cuaToi = new HashSet<Guid>();
        foreach (var p in db.PhanCong)
        {
            if (p is null || p.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            if (p.UserId == userId) cuaToi.Add(p.IdNvChiTiet);
        }

        var kq = new List<NhiemVuSapDenHanDto>();
        foreach (var nv in db.NhiemVu)
        {
            if (nv is null || !cuaToi.Contains(nv.Id)) continue;
            if (RecommendationService.DaKetThuc(nv)) continue;

            var conLai = NgayUtil.SoNgayConLai(nv.HanXuLyTh, homNay);
            if (!conLai.HasValue || conLai.Value > soNgay) continue;

            kq.Add(new NhiemVuSapDenHanDto
            {
                Id = nv.Id,
                NoiDung = nv.NoiDung,
                HanXuLyTh = nv.HanXuLyTh,
                SoNgayConLai = conLai,
                DoKhan = nv.DoKhan,
                TrangThai = nv.TrangThai,
                QuaHan = conLai.Value < 0
            });
        }

        kq.Sort((a, b) =>
        {
            var soSanh = (a.SoNgayConLai ?? int.MaxValue).CompareTo(b.SoNgayConLai ?? int.MaxValue);
            return soSanh != 0 ? soSanh : a.Id.CompareTo(b.Id);
        });
        return kq;
    }

    /// <summary>
    /// §5.9 I2 — nang luc suy dien tu lich su theo tung linh vuc (chi de xem, khong nhap tay).
    /// </summary>
    public static NangLucNguoiDungDto NangLuc(AiSnapshot db, Guid userId, CauHinhAiDto? cauHinh = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        var c = cauHinh ?? CauHinhAi.HienTai;
        var nguongCM = c.NguongChuyenMon > 0 ? c.NguongChuyenMon : 1;

        var kq = new NangLucNguoiDungDto { UserId = userId };
        foreach (var u in db.NguoiDung)
        {
            if (u is not null && u.Id == userId) { kq.FullName = u.FullName; break; }
        }

        var dem = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var nv in LayNhiemVuChuTri(db, userId))
        {
            if (!RecommendationService.DaNghiemThu(nv)) continue;
            var lv = string.IsNullOrWhiteSpace(nv.LinhVuc) ? "(chưa gán)" : nv.LinhVuc!;
            dem[lv] = (dem.TryGetValue(lv, out var d) ? d : 0) + 1;
            kq.TongSoHoanThanh++;
        }

        var tenLv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lv in db.LinhVuc)
        {
            if (lv is not null && !string.IsNullOrWhiteSpace(lv.Ma)) tenLv[lv.Ma] = lv.Ten;
        }

        foreach (var cap in dem)
        {
            kq.TheoLinhVuc.Add(new NangLucLinhVucDto
            {
                LinhVuc = cap.Key,
                TenLinhVuc = tenLv.TryGetValue(cap.Key, out var ten) ? ten : cap.Key,
                SoNvHoanThanh = cap.Value,
                DiemChuyenMon = SoHoc.LamTron(Math.Min(1.0, cap.Value / nguongCM), 2)
            });
        }
        kq.TheoLinhVuc.Sort((a, b) =>
        {
            var soSanh = b.SoNvHoanThanh.CompareTo(a.SoNvHoanThanh);
            return soSanh != 0 ? soSanh : string.CompareOrdinal(a.LinhVuc, b.LinhVuc);
        });
        return kq;
    }

    /// <summary>
    /// §5.9 I3 / §4.8 <c>USER_HIEUSUAT</c> — tinh THANG bang truy van, khong can bang dem.
    /// <paramref name="linhVuc"/> null = tong hop moi linh vuc.
    /// </summary>
    public static HieuSuatNguoiDungDto HieuSuat(AiSnapshot db, Guid userId, string? linhVuc)
    {
        ArgumentNullException.ThrowIfNull(db);
        var kq = new HieuSuatNguoiDungDto
        {
            UserId = userId,
            LinhVuc = string.IsNullOrWhiteSpace(linhVuc) ? null : linhVuc,
            K = GioiHan.MaxConcurrentTasksMacDinh
        };

        foreach (var u in db.NguoiDung)
        {
            if (u is null || u.Id != userId) continue;
            kq.FullName = u.FullName;
            if (u.MaxConcurrentTasks > 0) kq.K = u.MaxConcurrentTasks;
            break;
        }

        var soTraLaiTheoNv = new Dictionary<Guid, int>();
        foreach (var x in db.XuLy)
        {
            if (RecommendationService.LaBanGhiTraLai(x))
            {
                soTraLaiTheoNv[x!.IdCtnv] = (soTraLaiTheoNv.TryGetValue(x.IdCtnv, out var d) ? d : 0) + 1;
            }
        }

        foreach (var nv in LayNhiemVuChuTri(db, userId))
        {
            if (!string.IsNullOrWhiteSpace(linhVuc)
                && !string.Equals(nv.LinhVuc, linhVuc, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (RecommendationService.DaNghiemThu(nv))
            {
                kq.SoNvHoanThanh++;
                if (nv.TrangThai == TrangThaiNv.HoanThanh) kq.SoNvDungHan++;
            }
            kq.SoLanGiaHan += nv.SoLanGiaHan;
            kq.SoNvBiTraLai += soTraLaiTheoNv.TryGetValue(nv.Id, out var st) ? st : 0;

            if (RecommendationService.DangGiuViec(nv))
            {
                kq.SoNvDangMo++;
                kq.TaiTrongSo += CauHinhAi.TrongSoDoKhan(nv.DoKhan);
                if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan) kq.SoNvQuaHan++;
            }
        }

        kq.TaiTrongSo = SoHoc.LamTron(kq.TaiTrongSo, 2);
        kq.TyLeDungHan = SoHoc.LamTron(SoHoc.ChiaAnToan(kq.SoNvDungHan, kq.SoNvHoanThanh), 2);
        return kq;
    }

    /// <summary>Cac nhiem vu mot nguoi giu vai CHUTRI voi phan cong con hieu luc.</summary>
    private static List<DmNhiemVuChiTiet> LayNhiemVuChuTri(AiSnapshot db, Guid userId)
    {
        var idNv = new HashSet<Guid>();
        foreach (var p in db.PhanCong)
        {
            if (p is null || p.UserId != userId) continue;
            if (p.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;
            if (!string.Equals(p.VaiTro, VaiTroPhanCong.ChuTri, StringComparison.Ordinal)) continue;
            idNv.Add(p.IdNvChiTiet);
        }

        var kq = new List<DmNhiemVuChiTiet>();
        foreach (var nv in db.NhiemVu)
        {
            if (nv is not null && idNv.Contains(nv.Id)) kq.Add(nv);
        }
        return kq;
    }

    // ======================================================================
    // 4. §9.8 SO SANH BASELINE — danh gia "time-split", CHONG RO RI DU LIEU
    // ======================================================================

    /// <summary>Ma va nhan cua bon chien luoc so sanh (§9.8).</summary>
    private static readonly (string Ma, string Ten)[] BangChienLuoc =
    {
        ("MO_HINH_DAY_DU", "Mô hình chấm điểm có trọng số (AI)"),
        ("NGAU_NHIEN", "Chọn ngẫu nhiên"),
        ("RANH_NHAT", "Chọn người rảnh nhất (S4)"),
        ("CHUYEN_MON_CAO_NHAT", "Chọn người chuyên môn cao nhất (S1)")
    };

    /// <summary>
    /// §9.8 — so sanh mo hinh voi 3 baseline. Tra danh sach RONG khi khong du du lieu
    /// de danh gia (duoi 2 ung vien hop le, hoac chua co nhiem vu nao da nghiem thu).
    ///
    /// TON TRONG THU TU THOI GIAN — bon lop chan ro ri du lieu tuong lai:
    ///  L1. Cat theo NGAY: chi giu nhiem vu co ngay giao &lt; ngay giao cua X (so sanh CHAT,
    ///      loai ca nhiem vu giao cung ngay vi trong ngay khong co thu tu gio dang tin).
    ///  L2. Che ket qua tuong lai cua nhiem vu CU chua xong tai moc: ha ve (2, null).
    ///  L2b. Dung lai cac truong dong tai moc: trangthai 7 do job I5 chay SAU moc,
    ///      solangiahan / trangthaixulygiahan / hanxulyth do duyet gia han SAU moc.
    ///  L3. Cat bang phu thuoc (phancong, xuly, giahan, nhat ky) theo cung moc —
    ///      nho vay phan cong cua chinh X bien mat, khong chien luoc nao "nhin thay dap an".
    ///  L4. <see cref="AiSnapshot.HomNay"/> cua anh chup = ngay giao cua X.
    ///
    /// HAN CHE DA BIET: §4.3 NHIEMVU_PHANCONG chi co <c>createdate</c>, KHONG co moc thu hoi,
    /// nen ban ghi bi thu hoi SAU moc van bi coi la da thu hoi =&gt; tai tai moc co the thieu
    /// mot phan. Khong sua duoc bang du lieu hien co; ghi ro thay vi khang dinh da loc sach.
    /// </summary>
    public static IReadOnlyList<SoSanhBaselineDto> SoSanhBaseline(
        AiSnapshot db, int soMau = SoMauBaselineMacDinh, CauHinhAiDto? cauHinh = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        var c = cauHinh ?? CauHinhAi.HienTai;
        if (soMau <= 0) soMau = SoMauBaselineMacDinh;

        // Tap ung vien: TOAN BO nguoi thuc hien dang hoat dong, khong gioi han don vi —
        // moi chien luoc cung doi mat mot tap nhu nhau => so sanh cong bang.
        var ungVien = new List<SysUser>();
        foreach (var u in db.NguoiDung)
        {
            if (RecommendationService.LaUngVienHopLe(u)) ungVien.Add(u!);
        }
        if (ungVien.Count < 2) return Array.Empty<SoSanhBaselineDto>();

        var laUngVien = new HashSet<Guid>();
        foreach (var u in ungVien) laUngVien.Add(u.Id);

        // "Dap an": nhiem vu DA NGHIEM THU, co nguoi chu tri con la ung vien hop le
        var mau = new List<(DmNhiemVuChiTiet Nv, Guid NguoiThat)>();
        foreach (var nv in db.NhiemVu)
        {
            if (!RecommendationService.DaNghiemThu(nv)) continue;
            var nguoiThat = NguoiChuTri(db.PhanCong, nv!.Id);
            // Bo mau ma nguoi that hien khong con la ung vien (da khoa / doi vai):
            // khong chien luoc nao tim ra duoc, giu lai chi lam nhieu deu ca bon dong.
            if (nguoiThat is null || !laUngVien.Contains(nguoiThat.Value)) continue;
            mau.Add((nv, nguoiThat.Value));
        }
        if (mau.Count == 0) return Array.Empty<SoSanhBaselineDto>();

        mau.Sort((a, b) =>
        {
            var soSanh = a.Nv.NgayGiao.CompareTo(b.Nv.NgayGiao);
            return soSanh != 0 ? soSanh : a.Nv.Id.CompareTo(b.Nv.Id);
        });

        // Lay mau DEU theo thoi gian de khong thien ve giai doan nao
        var mauChon = new List<(DmNhiemVuChiTiet Nv, Guid NguoiThat)>();
        if (mau.Count <= soMau)
        {
            mauChon.AddRange(mau);
        }
        else
        {
            var buoc = mau.Count / (double)soMau;
            for (var i = 0; i < soMau; i++)
            {
                var vt = (int)Math.Floor(i * buoc);
                if (vt > mau.Count - 1) vt = mau.Count - 1;
                mauChon.Add(mau[vt]);
            }
        }

        // Bo dem ket qua cho tung chien luoc
        var hang1 = new int[BangChienLuoc.Length];
        var top3 = new int[BangChienLuoc.Length];
        var nghichDao = new double[BangChienLuoc.Length];
        var phanBo = new Dictionary<Guid, int>[BangChienLuoc.Length];
        for (var i = 0; i < BangChienLuoc.Length; i++) phanBo[i] = new Dictionary<Guid, int>();

        var soMauDaChay = 0;
        for (var iMau = 0; iMau < mauChon.Count; iMau++)
        {
            var x = mauChon[iMau].Nv;
            var uidThat = mauChon[iMau].NguoiThat;

            var anh = CatTheoMoc(db, DateOnly.FromDateTime(x.NgayGiao), x);
            var cm = ChiMucAi.Xay(anh);

            var yeuCau = new GoiYRequest
            {
                IdNvChiTiet = x.Id,
                NoiDung = x.NoiDung,
                LinhVuc = x.LinhVuc,
                DoKhan = x.DoKhan,
                HanXuLyTh = x.HanXuLyTh,
                SoLuong = ungVien.Count
            };

            // Diem S1 / S4 tai thoi diem anh chup — dung CHINH cong thuc §9.4
            var s1 = new Dictionary<Guid, double>();
            var s4 = new Dictionary<Guid, double>();
            foreach (var u in ungVien)
            {
                var dt = RecommendationService.TinhDacTrung(anh, u, yeuCau, c, cm);
                s1[u.Id] = dt.S1;
                s4[u.Id] = dt.S4;
            }

            // (a) MO_HINH — chay dong co tren ANH CHUP, khong phai du lieu hien tai
            var goiY = RecommendationService.GoiY(anh, yeuCau, c);
            var xepMoHinh = new List<Guid>();
            foreach (var uv in goiY.UngVien)
            {
                if (laUngVien.Contains(uv.UserId)) xepMoHinh.Add(uv.UserId);
            }

            // Cat moi chien luoc o cung do dai K de so sanh cong bang
            var k = xepMoHinh.Count > 0 ? Math.Max(3, xepMoHinh.Count) : ungVien.Count;

            var xep = new List<Guid>[BangChienLuoc.Length];
            xep[0] = CatDau(xepMoHinh, k);
            xep[1] = CatDau(XaoTronCoSeed(ungVien, SeedBaseline + (uint)(iMau * 7919)), k);
            xep[2] = CatDau(XepTheoDiem(ungVien, u => s4[u.Id]), k);
            xep[3] = CatDau(XepTheoDiem(ungVien, u => s1[u.Id]), k);

            for (var iCl = 0; iCl < BangChienLuoc.Length; iCl++)
            {
                var dsXep = xep[iCl];
                if (dsXep.Count == 0) continue;

                var hang = ThuHangCua(dsXep, uidThat);
                if (hang == 1) hang1[iCl]++;
                if (hang is >= 1 and <= 3) top3[iCl]++;
                if (hang >= 1) nghichDao[iCl] += 1.0 / hang;

                // Mo phong giao viec cho ung vien hang 1 de do do dong deu tai
                var dau = dsXep[0];
                phanBo[iCl][dau] = (phanBo[iCl].TryGetValue(dau, out var dem) ? dem : 0) + 1;
            }
            soMauDaChay++;
        }

        if (soMauDaChay == 0) return Array.Empty<SoSanhBaselineDto>();

        var kq = new List<SoSanhBaselineDto>();
        for (var iCl = 0; iCl < BangChienLuoc.Length; iCl++)
        {
            // Mau so la TONG so mau da chay: chien luoc nao khong xep duoc lan nao thi lan do
            // tinh la truot — khong duoc "thuong" bang cach thu nho mau so.
            var dsTai = new List<double>(ungVien.Count);
            foreach (var u in ungVien)
            {
                dsTai.Add(phanBo[iCl].TryGetValue(u.Id, out var dem) ? dem : 0);
            }

            kq.Add(new SoSanhBaselineDto
            {
                ChienLuoc = BangChienLuoc[iCl].Ma,
                Nhan = BangChienLuoc[iCl].Ten,
                Precision1 = SoHoc.LamTron(SoHoc.ChiaAnToan(hang1[iCl], soMauDaChay), 2),
                Precision3 = SoHoc.LamTron(SoHoc.ChiaAnToan(top3[iCl], soMauDaChay), 2),
                Mrr = SoHoc.LamTron(SoHoc.ChiaAnToan(nghichDao[iCl], soMauDaChay), 2),
                GiniTai = Gini(dsTai)
            });
        }
        return kq;
    }

    /// <summary>Nguoi giu vai CHUTRI cua mot nhiem vu (uu tien ban ghi con hieu luc, som nhat).</summary>
    private static Guid? NguoiChuTri(IReadOnlyList<NhiemVuPhanCong> dsPhanCong, Guid idNv)
    {
        NhiemVuPhanCong? uuTien = null;
        NhiemVuPhanCong? duPhong = null;
        foreach (var p in dsPhanCong)
        {
            if (p is null || p.IdNvChiTiet != idNv) continue;
            if (!string.Equals(p.VaiTro, VaiTroPhanCong.ChuTri, StringComparison.Ordinal)) continue;

            if (p.TrangThai == TrangThaiPhanCong.ConHieuLuc)
            {
                if (uuTien is null || p.CreateDate < uuTien.CreateDate) uuTien = p;
            }
            else if (duPhong is null || p.CreateDate < duPhong.CreateDate)
            {
                duPhong = p;
            }
        }
        var chon = uuTien ?? duPhong;
        return chon?.UserId;
    }

    /// <summary>
    /// Dung anh chup CSDL tai <paramref name="moc"/> (L1..L4 mo ta o <see cref="SoSanhBaseline"/>).
    /// <paramref name="nhiemVuDangXet"/> khac null thi them mot ban sao TRUNG TINH cua no
    /// (trangthai = 3, chua phan cong) de tra cuu theo id van thay, ma khong cong diem cho ai.
    /// </summary>
    private static AiSnapshot CatTheoMoc(AiSnapshot db, DateOnly moc, DmNhiemVuChiTiet? nhiemVuDangXet)
    {
        var giuId = new HashSet<Guid>();
        var dsNv = new List<DmNhiemVuChiTiet>();

        foreach (var nv in db.NhiemVu)
        {
            if (nv is null) continue;
            if (nhiemVuDangXet is not null && nv.Id == nhiemVuDangXet.Id) continue;
            if (!(DateOnly.FromDateTime(nv.NgayGiao) < moc)) continue;   // L1

            var b = SaoChep(nv);
            var ketThuc = b.NgayHoanThanhThucTe ?? b.UpdateDate;
            var xongTruocMoc = RecommendationService.DaKetThuc(b) && DateOnly.FromDateTime(ketThuc) < moc;

            // L2 — che ket qua tuong lai cua nhiem vu chua ket thuc tai moc
            if (RecommendationService.DaKetThuc(b) && !xongTruocMoc)
            {
                b.TrangThai = TrangThaiNv.DangTrienKhai;
                b.TrangThaiDvXuly = null;
                b.TrangThaiXuLyGiaHan = null;
                b.NgayHoanThanhThucTe = null;
                b.SoLanGiaHan = 0;
                b.MucDoHt = 0;
            }

            // L2b — dung lai cac truong dong cua nhiem vu CON MO tai moc
            if (!xongTruocMoc)
            {
                var gh = GiaHanTaiMoc(db.GiaHan, b.Id, moc);
                b.SoLanGiaHan = gh.SoLan;                                  // §9.4 S3(c)
                if (gh.HanCu.HasValue) b.HanXuLyTh = gh.HanCu;             // han bi noi SAU moc
                b.TrangThaiXuLyGiaHan = gh.ChoDuyet ? TrangThaiGiaHan.ChoDuyet : (int?)null;
                b.TrangThaiDvXuly = null;
                b.NgayHoanThanhThucTe = null;
                b.MucDoHt = 0;
                if (b.NgayTiepNhan.HasValue && !(DateOnly.FromDateTime(b.NgayTiepNhan.Value) < moc))
                {
                    b.NgayTiepNhan = null;
                }
                // Ngay tu choi khong co trong §4.2 => giu nguyen ma 6 neu nhiem vu da bi tu choi
                if (b.TrangThai != TrangThaiNv.TuChoi)
                {
                    b.TrangThai = gh.ChoDuyet
                        ? TrangThaiNv.GiaHan                                            // §2.4 T11
                        : (b.HanXuLyTh.HasValue && b.HanXuLyTh.Value < moc
                            ? TrangThaiNv.DangTrienKhaiQuaHan                           // §2.5 job 2|3 -> 7
                            : (b.NgayTiepNhan.HasValue
                                ? TrangThaiNv.DangTrienKhai                             // §2.4 T2
                                : TrangThaiNv.ChuaTrienKhai));                          // §2.4 T1
                }
            }

            giuId.Add(b.Id);
            dsNv.Add(b);
        }

        if (nhiemVuDangXet is not null)
        {
            var x = SaoChep(nhiemVuDangXet);
            x.TrangThai = TrangThaiNv.ChuaTrienKhai;
            x.TrangThaiDvXuly = null;
            x.TrangThaiXuLyGiaHan = null;
            x.SoLanGiaHan = 0;
            x.MucDoHt = 0;
            x.NgayTiepNhan = null;
            x.NgayHoanThanhThucTe = null;
            x.AiGoiYId = null;
            dsNv.Add(x);
        }

        // L3 — cat cac bang phu thuoc theo cung moc
        var dsPc = new List<NhiemVuPhanCong>();
        foreach (var p in db.PhanCong)
        {
            if (p is null || !giuId.Contains(p.IdNvChiTiet)) continue;
            if (DateOnly.FromDateTime(p.CreateDate) < moc) dsPc.Add(p);
        }

        var dsXl = new List<XuLyNhiemVu>();
        foreach (var x in db.XuLy)
        {
            if (x is null || !giuId.Contains(x.IdCtnv)) continue;
            if (DateOnly.FromDateTime(x.NgayXuLy) < moc) dsXl.Add(x);
        }

        var dsGh = new List<GiaHanNhiemVu>();
        foreach (var g in db.GiaHan)
        {
            if (g is null || !giuId.Contains(g.IdCtnv)) continue;
            if (DateOnly.FromDateTime(g.CreateDate) < moc) dsGh.Add(g);
        }

        var dsLog = new List<AiGoiYLog>();
        foreach (var l in db.NhatKyAi)
        {
            if (l is not null && DateOnly.FromDateTime(l.CreateDate) < moc) dsLog.Add(l);
        }

        return new AiSnapshot
        {
            HomNay = moc,                       // L4
            NguoiDung = db.NguoiDung,
            LinhVuc = db.LinhVuc,
            DonVi = db.DonVi,
            NhiemVu = dsNv,
            PhanCong = dsPc,
            XuLy = dsXl,
            GiaHan = dsGh,
            NhatKyAi = dsLog
        };
    }

    /// <summary>
    /// L2b — trang thai GIA HAN cua mot nhiem vu dung tai <paramref name="moc"/> (§4.5, §2.4 T11-T13).
    /// </summary>
    /// <returns>
    /// <c>SoLan</c> = so lan gia han DA DUOC DUYET truoc moc (§9.4 S3c chi duoc cong chung nay);
    /// <c>HanCu</c> = <c>hanxulyth_cu</c> cua lan duyet SOM NHAT sau moc, chinh la han tai moc;
    /// <c>ChoDuyet</c> = co de xuat tao truoc moc ma den moc van chua duoc xu ly.
    /// </returns>
    private static (int SoLan, DateOnly? HanCu, bool ChoDuyet) GiaHanTaiMoc(
        IReadOnlyList<GiaHanNhiemVu> dsGiaHan, Guid idNv, DateOnly moc)
    {
        var soLan = 0;
        DateOnly? hanCu = null;
        DateTime? ngayDuyetSom = null;
        var choDuyet = false;

        foreach (var g in dsGiaHan)
        {
            if (g is null || g.IdCtnv != idNv) continue;
            if (!(DateOnly.FromDateTime(g.CreateDate) < moc)) continue;   // de xuat tao sau moc

            var daXuLyTruocMoc = g.NgayDuyet.HasValue
                                 && DateOnly.FromDateTime(g.NgayDuyet.Value) < moc;
            if (daXuLyTruocMoc)
            {
                if (g.TrangThai == TrangThaiGiaHan.DaDuyet) soLan++;
            }
            else
            {
                choDuyet = true;   // tai moc van dang cho nguoi giao xu ly
                if (g.TrangThai == TrangThaiGiaHan.DaDuyet
                    && g.HanXuLyThCu.HasValue
                    && g.NgayDuyet.HasValue
                    && (ngayDuyetSom is null || g.NgayDuyet.Value < ngayDuyetSom.Value))
                {
                    ngayDuyetSom = g.NgayDuyet;
                    hanCu = g.HanXuLyThCu;
                }
            }
        }

        return (soLan, hanCu, choDuyet);
    }

    /// <summary>Sao chep NONG mot nhiem vu (bo qua thuoc tinh dieu huong) de khong cham du lieu goc.</summary>
    private static DmNhiemVuChiTiet SaoChep(DmNhiemVuChiTiet nv) => new()
    {
        Id = nv.Id,
        IdVb = nv.IdVb,
        NoiDung = nv.NoiDung,
        LinhVuc = nv.LinhVuc,
        DoKhan = nv.DoKhan,
        HanXuLyTh = nv.HanXuLyTh,
        SoNgayHxlTh = nv.SoNgayHxlTh,
        HanXuLyPh = nv.HanXuLyPh,
        NgayGiao = nv.NgayGiao,
        NgayTiepNhan = nv.NgayTiepNhan,
        NgayHoanThanhThucTe = nv.NgayHoanThanhThucTe,
        TrangThai = nv.TrangThai,
        TrangThaiDvXuly = nv.TrangThaiDvXuly,
        TrangThaiXuLyGiaHan = nv.TrangThaiXuLyGiaHan,
        SoLanGiaHan = nv.SoLanGiaHan,
        MucDoHt = nv.MucDoHt,
        PhanHoi = nv.PhanHoi,
        HsChatLuong = nv.HsChatLuong,
        UserIdGiaoViec = nv.UserIdGiaoViec,
        UserIdCreate = nv.UserIdCreate,
        UnitCode = nv.UnitCode,
        CreateDate = nv.CreateDate,
        UpdateDate = nv.UpdateDate,
        AiGoiYId = nv.AiGoiYId
    };

    /// <summary>Xep giam dan theo diem; hoa diem thi theo id de ket qua TAI LAP duoc.</summary>
    private static List<Guid> XepTheoDiem(IReadOnlyList<SysUser> ungVien, Func<SysUser, double> chamDiem)
    {
        var ds = new List<(Guid Id, double Diem)>(ungVien.Count);
        foreach (var u in ungVien) ds.Add((u.Id, chamDiem(u)));
        ds.Sort((a, b) =>
        {
            var soSanh = b.Diem.CompareTo(a.Diem);
            return soSanh != 0 ? soSanh : a.Id.CompareTo(b.Id);
        });

        var kq = new List<Guid>(ds.Count);
        foreach (var x in ds) kq.Add(x.Id);
        return kq;
    }

    /// <summary>Xao tron Fisher-Yates voi PRNG CO SEED — khong dung so ngau nhien that (§9.9).</summary>
    private static List<Guid> XaoTronCoSeed(IReadOnlyList<SysUser> ungVien, uint seed)
    {
        var ds = new List<Guid>(ungVien.Count);
        foreach (var u in ungVien) ds.Add(u.Id);

        var rnd = new Mulberry32(seed);
        for (var i = ds.Count - 1; i > 0; i--)
        {
            var j = (int)Math.Floor(rnd.Ke() * (i + 1));
            if (j > i) j = i;
            (ds[i], ds[j]) = (ds[j], ds[i]);
        }
        return ds;
    }

    private static List<Guid> CatDau(List<Guid> ds, int k)
    {
        if (k >= ds.Count) return ds;
        return ds.GetRange(0, Math.Max(0, k));
    }

    /// <summary>Thu hang 1-based cua mot nguoi trong danh sach xep; 0 = khong co mat.</summary>
    private static int ThuHangCua(List<Guid> danhSach, Guid userId)
    {
        for (var i = 0; i < danhSach.Count; i++)
        {
            if (danhSach[i] == userId) return i + 1;
        }
        return 0;
    }

    /// <summary>
    /// PRNG mulberry32 — cung mot seed cho ra cung mot day. Dung cho baseline "chon ngau nhien"
    /// de ket qua trong bao cao thuc tap TAI LAP DUOC (§9.9: khong dung <c>Random</c> khong seed).
    /// </summary>
    private sealed class Mulberry32
    {
        private uint _a;

        public Mulberry32(uint seed) => _a = seed;

        public double Ke()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                var t = _a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return ((t ^ (t >> 14)) & 0xFFFFFFFFu) / 4294967296.0;
            }
        }
    }

    // ======================================================================
    // 5. §3.4 M13 — NHAT KY GOI Y AI
    // ======================================================================

    /// <summary>§3.4 M13 — nhat ky goi y, moi nhat truoc, phan trang trong bo nho.</summary>
    public static PagedResult<AiGoiYLogDto> NhatKy(AiSnapshot db, int trang, int kichThuoc)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (trang < 1) trang = 1;
        if (kichThuoc < 1) kichThuoc = GioiHan.KichThuocTrangMacDinh;
        if (kichThuoc > GioiHan.KichThuocTrangToiDa) kichThuoc = GioiHan.KichThuocTrangToiDa;

        var tenNguoi = new Dictionary<Guid, string>();
        foreach (var u in db.NguoiDung)
        {
            if (u is not null) tenNguoi[u.Id] = u.FullName;
        }

        var noiDungNv = new Dictionary<Guid, string>();
        foreach (var nv in db.NhiemVu)
        {
            if (nv is not null) noiDungNv[nv.Id] = nv.NoiDung;
        }

        var dsSapXep = new List<AiGoiYLog>();
        foreach (var log in db.NhatKyAi)
        {
            if (log is not null) dsSapXep.Add(log);
        }
        dsSapXep.Sort((a, b) =>
        {
            var soSanh = b.CreateDate.CompareTo(a.CreateDate);
            return soSanh != 0 ? soSanh : b.Id.CompareTo(a.Id);
        });

        var tongSo = dsSapXep.Count;
        var boQua = (trang - 1) * kichThuoc;
        var items = new List<AiGoiYLogDto>();
        for (var i = boQua; i < tongSo && items.Count < kichThuoc; i++)
        {
            var log = dsSapXep[i];

            string? noiDung = null;
            if (log.IdNvChiTiet.HasValue && noiDungNv.TryGetValue(log.IdNvChiTiet.Value, out var nd)) noiDung = nd;

            string? tenDaChon = null;
            if (log.UserIdDaChon.HasValue && tenNguoi.TryGetValue(log.UserIdDaChon.Value, out var ten)) tenDaChon = ten;

            var dto = new AiGoiYLogDto
            {
                Id = log.Id,
                IdNvChiTiet = log.IdNvChiTiet,
                NoiDungNhiemVu = noiDung,
                LinhVuc = log.LinhVuc,
                UserIdDaChon = log.UserIdDaChon,
                NguoiDaChonTen = tenDaChon,
                ThuHangDaChon = log.ThuHangDaChon,
                CoTrongGoiY = log.CoTrongGoiY,
                PhienBanTrongSo = log.PhienBanTrongSo,
                CheDo = log.CheDo,
                CreateDate = log.CreateDate
            };

            foreach (var uv in RecommendationService.DocKetQuaJson(log.KetQuaJson))
            {
                dto.UngVien.Add(new UngVienTomTatLogDto
                {
                    ThuHang = uv.ThuHang,
                    UserId = uv.UserId,
                    FullName = uv.FullName,
                    DiemTong = uv.DiemTong,
                    DoTinCay = uv.DoTinCay,
                    Nhan = uv.Nhan
                });
            }
            items.Add(dto);
        }

        return new PagedResult<AiGoiYLogDto>(items, tongSo, trang, kichThuoc);
    }

    // ======================================================================
    // 6. CAI DAT IMetricsService
    // ======================================================================

    /// <inheritdoc />
    public async Task<ThongKeAiDto> ThongKeAiAsync(CancellationToken ct)
    {
        var db = await _nguon.LaySnapshotAsync(true, ct).ConfigureAwait(false);
        return ThongKeAi(db);
    }

    /// <inheritdoc />
    public async Task<double> GiniTaiAsync(CancellationToken ct)
    {
        var db = await _nguon.LaySnapshotAsync(false, ct).ConfigureAwait(false);
        return GiniTai(db);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SoSanhBaselineDto>> SoSanhBaselineAsync(CancellationToken ct)
    {
        var db = await _nguon.LaySnapshotAsync(true, ct).ConfigureAwait(false);
        return SoSanhBaseline(db, SoMauBaselineMacDinh, CauHinhAi.HienTai);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AiGoiYLogDto>> NhatKyAsync(int trang, int kichThuoc, CancellationToken ct)
    {
        var db = await _nguon.LaySnapshotAsync(true, ct).ConfigureAwait(false);
        return NhatKy(db, trang, kichThuoc);
    }
}
