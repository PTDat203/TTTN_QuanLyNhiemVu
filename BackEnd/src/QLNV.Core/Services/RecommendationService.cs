using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>
/// Cac con so trung gian cua §9.4 — giu lai de sinh cau ly do (§9.6) va de nguoi giao
/// kiem chung tung buoc tinh (§9.1 "giai thich duoc").
/// </summary>
public sealed class CoSoTinhAi
{
    /// <summary>Ma linh vuc cua nhiem vu dang giao. Null = nhiem vu chua gan linh vuc.</summary>
    public string? MaLinhVuc { get; set; }

    /// <summary>Ten linh vuc de hien thi (tra tu DM_LINHVUC).</summary>
    public string? TenLinhVuc { get; set; }

    /// <summary>§9.5 — nhiem vu khong co linh vuc thi S1 tinh tren TONG so nhiem vu.</summary>
    public bool KhongCoLinhVuc { get; set; }

    /// <summary>Nhom cha cua linh vuc (§9.4 S1 nhanh n = 0).</summary>
    public string? MaNhomCha { get; set; }

    /// <summary>Ten nhom cha de hien thi.</summary>
    public string? TenNhomCha { get; set; }

    /// <summary>§9.4 S1 — <c>n</c>: so nhiem vu da nghiem thu DUNG linh vuc L.</summary>
    public int SoNvLinhVuc { get; set; }

    /// <summary>§9.4 S1 — <c>n'</c>: so nhiem vu da nghiem thu o linh vuc CUNG NHOM CHA.</summary>
    public int SoNvNhomCha { get; set; }

    /// <summary>Gia tri thuc su dua vao cong thuc S1.</summary>
    public int SoNvDungChoS1 { get; set; }

    /// <summary>§9.4 S1 — co ap dung chiet khau 50% cua nhanh nhom cha khong.</summary>
    public bool DungChietKhauNhomCha { get; set; }

    /// <summary>§9.4 S2 — <c>N</c>: TONG so nhiem vu da nghiem thu (moi linh vuc).</summary>
    public int SoNvHoanThanhTong { get; set; }

    /// <summary>§9.4 S3(a) — <c>r_dunghan</c> sau khi lam muot Laplace.</summary>
    public double RDungHan { get; set; }

    /// <summary>§9.4 S3(b) — <c>tyle_tralai</c>.</summary>
    public double TyLeTraLai { get; set; }

    /// <summary>§9.4 S3(c) — <c>tyle_giahan</c>.</summary>
    public double TyLeGiaHan { get; set; }

    /// <summary>§9.4 S4 — tai co trong so do khan.</summary>
    public double Tai { get; set; }

    /// <summary>§9.4 S4 — nguong K (<c>max_concurrent_tasks</c>).</summary>
    public int K { get; set; }

    /// <summary>§9.4 S5 — <c>q</c>: so nhiem vu dang o trangthai = 7.</summary>
    public int SoNvQuaHan { get; set; }

    /// <summary>Nguong chuyen mon dang ap dung (§9.4 S1).</summary>
    public double NguongChuyenMon { get; set; }

    /// <summary>Nguong lich su dang ap dung (§9.4 S2).</summary>
    public double NguongLichSu { get; set; }

    /// <summary>Da co du lieu nghiem thu de tinh hieu qua chua (§9.5 cold start).</summary>
    public bool CoDuLieuHieuQua { get; set; }
}

/// <summary>
/// §9.4 — ket qua cham 5 dac trung cua MOT ung vien. Doi tuong THUAN DU LIEU,
/// khong chua logic, de kiem thu doi chieu tung con so voi dac ta.
/// </summary>
public sealed class DacTrungUngVien
{
    /// <summary>§9.4 S1 Chuyen mon, thuoc [0, 1].</summary>
    public double S1 { get; set; }

    /// <summary>§9.4 S2 Lich su thuc hien, thuoc [0, 1].</summary>
    public double S2 { get; set; }

    /// <summary>§9.4 S3 Hieu qua cong viec, thuoc [0, 1].</summary>
    public double S3 { get; set; }

    /// <summary>§9.4 S4 Khoi luong hien tai, thuoc [0, 1].</summary>
    public double S4 { get; set; }

    /// <summary>§9.4 S5 Tinh san sang, thuoc [0, 1].</summary>
    public double S5 { get; set; }

    /// <summary>So lieu tho tra ve cho FE (§9.6 <c>soLieu</c>).</summary>
    public SoLieuDto SoLieu { get; set; } = new();

    /// <summary>Cac con so trung gian, dung de sinh ly do va de go loi.</summary>
    public CoSoTinhAi CoSoTinh { get; set; } = new();
}

/// <summary>
/// Chi muc tam trong bo nho cho MOT lan goi §9.6 H1. Xay mot lan, dung cho moi ung vien:
/// do phuc tap O(N + P + X). Khong ghi gi vao CSDL.
/// </summary>
public sealed class ChiMucAi
{
    /// <summary>Nhiem vu theo khoa chinh.</summary>
    public Dictionary<Guid, DmNhiemVuChiTiet> NhiemVuTheoId { get; } = new();

    /// <summary>Cac nhiem vu moi nguoi dang / da giu vai CHUTRI (phan cong con hieu luc).</summary>
    public Dictionary<Guid, List<DmNhiemVuChiTiet>> ChuTriTheoUser { get; } = new();

    /// <summary>Phan cong con hieu luc cua tung nhiem vu (§9.3 dieu 4).</summary>
    public Dictionary<Guid, List<NhiemVuPhanCong>> PhanCongTheoNv { get; } = new();

    /// <summary>Nhung nguoi da tung TU CHOI tung nhiem vu (§9.3 dieu 5).</summary>
    public Dictionary<Guid, HashSet<Guid>> TuChoiTheoNv { get; } = new();

    /// <summary>So lan bi cham CHUA DAT cua tung nhiem vu (§9.4 S3b).</summary>
    public Dictionary<Guid, int> SoTraLaiTheoNv { get; } = new();

    /// <summary>Danh muc linh vuc theo ma.</summary>
    public Dictionary<string, DmLinhVuc> LinhVucTheoMa { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Danh muc don vi theo ma.</summary>
    public Dictionary<string, SysUnit> DonViTheoMa { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// §9.5 dong cuoi — toan he thong da co nhiem vu nao duoc NGHIEM THU chua.
    /// <c>false</c> =&gt; chuyen sang che do du phong KHOI_TAO.
    /// </summary>
    public bool HeThongCoNghiemThu { get; set; }

    /// <summary>Dung chi muc tu mot anh chup du lieu.</summary>
    public static ChiMucAi Xay(AiSnapshot db)
    {
        ArgumentNullException.ThrowIfNull(db);
        var cm = new ChiMucAi();

        foreach (var nv in db.NhiemVu)
        {
            if (nv is null) continue;
            cm.NhiemVuTheoId[nv.Id] = nv;
            if (RecommendationService.DaNghiemThu(nv)) cm.HeThongCoNghiemThu = true;
        }

        foreach (var lv in db.LinhVuc)
        {
            if (lv is null || string.IsNullOrWhiteSpace(lv.Ma)) continue;
            cm.LinhVucTheoMa[lv.Ma] = lv;
        }

        foreach (var dv in db.DonVi)
        {
            if (dv is null || string.IsNullOrWhiteSpace(dv.UnitCode)) continue;
            cm.DonViTheoMa[dv.UnitCode] = dv;
        }

        foreach (var p in db.PhanCong)
        {
            // §4.3: trangthai = 1 la con hieu luc, 0 la da thu hoi
            if (p is null || p.TrangThai != TrangThaiPhanCong.ConHieuLuc) continue;

            if (!cm.PhanCongTheoNv.TryGetValue(p.IdNvChiTiet, out var dsPc))
            {
                dsPc = new List<NhiemVuPhanCong>();
                cm.PhanCongTheoNv[p.IdNvChiTiet] = dsPc;
            }
            dsPc.Add(p);

            if (!string.Equals(p.VaiTro, VaiTroPhanCong.ChuTri, StringComparison.Ordinal)) continue;
            if (!cm.NhiemVuTheoId.TryGetValue(p.IdNvChiTiet, out var nvCuaPc)) continue;

            if (!cm.ChuTriTheoUser.TryGetValue(p.UserId, out var dsNv))
            {
                dsNv = new List<DmNhiemVuChiTiet>();
                cm.ChuTriTheoUser[p.UserId] = dsNv;
            }
            dsNv.Add(nvCuaPc);
        }

        foreach (var x in db.XuLy)
        {
            if (x is null) continue;

            // §9.3 dieu 5 — da tung TU CHOI chinh nhiem vu nay
            if (string.Equals(x.Loai, LoaiXuLy.TuChoi, StringComparison.Ordinal))
            {
                if (!cm.TuChoiTheoNv.TryGetValue(x.IdCtnv, out var tap))
                {
                    tap = new HashSet<Guid>();
                    cm.TuChoiTheoNv[x.IdCtnv] = tap;
                }
                tap.Add(x.UserIdXuLy);
            }

            // §9.4 S3(b) — dem ban ghi bi cham CHUA DAT
            if (RecommendationService.LaBanGhiTraLai(x))
            {
                cm.SoTraLaiTheoNv[x.IdCtnv] =
                    (cm.SoTraLaiTheoNv.TryGetValue(x.IdCtnv, out var dem) ? dem : 0) + 1;
            }
        }

        return cm;
    }
}

/// <summary>§9.3 — ket qua bo loc cung, chay TRUOC khi cham diem.</summary>
public sealed class KetQuaLocCung
{
    /// <summary>Cac ung vien qua duoc 5 dieu loc dau (dieu 6 KHONG loai ai).</summary>
    public List<SysUser> UngVien { get; } = new();

    /// <summary>Nhung nguoi bi loai, kem ly do tieng Viet.</summary>
    public List<UngVienBiLoaiDto> LoaiBo { get; } = new();

    /// <summary>§9.3 dieu 6 — cac userid bi coi la qua tai nghiem trong (van hien thi, xep cuoi).</summary>
    public HashSet<Guid> QuaTai { get; } = new();

    /// <summary>Dac trung da cham san trong luc loc — dung lai o buoc cham diem cho nhanh.</summary>
    public Dictionary<Guid, DacTrungUngVien> DacTrung { get; } = new();

    /// <summary>
    /// §9.3 dieu 4 + 5 co du thong tin de ap dung khong (request co <c>idnvchitiet</c>
    /// hoac co <c>daPhanCong</c> / <c>daTuChoi</c>).
    /// </summary>
    public bool CoLocTrungLap { get; set; }
}

/// <summary>
/// §4.8 <c>AI_GOIY_LOG.ket_qua_json</c> — mot dong trong nhat ky goi y.
/// Giu dung ten khoa JSON de <see cref="MetricsService"/> doc lai duoc va de
/// <see cref="UngVienTomTatLogDto"/> (§3.4 M13) anh xa 1-1 (bo qua <c>diemThanhPhan</c>).
/// </summary>
public sealed class UngVienNhatKyJson
{
    [JsonPropertyName("thuHang")]
    public int ThuHang { get; set; }

    [JsonPropertyName("userid")]
    public Guid UserId { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("diemTong")]
    public double DiemTong { get; set; }

    [JsonPropertyName("doTinCay")]
    public double DoTinCay { get; set; }

    [JsonPropertyName("diemThanhPhan")]
    public DiemThanhPhanDto? DiemThanhPhan { get; set; }

    [JsonPropertyName("nhan")]
    public List<string> Nhan { get; set; } = new();
}

/// <summary>
/// §9 — DONG CO GOI Y NGUOI THUC HIEN. Trong tam cua de tai.
///
/// Ban chat: he thong CHAM DIEM DA TIEU CHI CO TRONG SO (MCDM / rule-based).
/// KHONG dung LLM, KHONG goi API ngoai, KHONG dung so ngau nhien (§9.9 ghi chu).
///
/// Toan bo thuat toan nam trong cac ham STATIC THUAN TUY (<see cref="GoiY"/>,
/// <see cref="TinhDacTrung"/>, <see cref="LocCung"/>) chay tren <see cref="AiSnapshot"/>,
/// nen kiem thu duoc ma khong can CSDL. Phan bat dong bo chi lam viec nap du lieu
/// va ghi nhat ky.
/// </summary>
public sealed class RecommendationService : IRecommendationService
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        // Giu tieng Viet co dau nguyen ven trong ket_qua_json (khong bi \uXXXX hoa)
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly IAiDataSource _nguon;
    private readonly IHienTai? _hienTai;

    /// <summary>Khoi tao khong co ngu canh dang nhap (dung cho job nen va kiem thu).</summary>
    public RecommendationService(IAiDataSource nguon)
        : this(nguon, null)
    {
    }

    /// <summary>Khoi tao day du.</summary>
    /// <param name="nguon">Cong doc/ghi du lieu, do tang Infrastructure cai dat.</param>
    /// <param name="hienTai">Ngu canh nguoi dang dang nhap — chi de ghi <c>AI_GOIY_LOG.userid_goiy</c>.</param>
    public RecommendationService(IAiDataSource nguon, IHienTai? hienTai)
    {
        _nguon = nguon ?? throw new ArgumentNullException(nameof(nguon));
        _hienTai = hienTai;
    }

    // ======================================================================
    // 1. VI NGU TRANG THAI (§2.1, §2.6, §4.8) — dung chung voi MetricsService
    // ======================================================================

    /// <summary>§2.6 diem cuoi — trangthai thuoc {1, 5} VA trangthaiDvXuly = 11.</summary>
    public static bool DaNghiemThu(DmNhiemVuChiTiet? nv) =>
        nv is not null
        && (nv.TrangThai == TrangThaiNv.HoanThanh || nv.TrangThai == TrangThaiNv.HoanThanhSauHan)
        && nv.TrangThaiDvXuly == TrangThaiPh.DaXacNhan;

    /// <summary>§2.6 — hai diem cuoi: da nghiem thu hoac da thu hoi (97).</summary>
    public static bool DaKetThuc(DmNhiemVuChiTiet? nv) =>
        DaNghiemThu(nv) || (nv is not null && nv.TrangThai == TrangThaiNv.DaThuHoi);

    /// <summary>§4.8 view <c>USER_TAI_HIENTAI</c> — dang giu viec: trangthai NOT IN (1, 5, 97).</summary>
    public static bool DangGiuViec(DmNhiemVuChiTiet? nv) =>
        nv is not null
        && nv.TrangThai != TrangThaiNv.HoanThanh
        && nv.TrangThai != TrangThaiNv.HoanThanhSauHan
        && nv.TrangThai != TrangThaiNv.DaThuHoi;

    /// <summary>
    /// §9.4 S3(b) — ban ghi xu ly bi cham CHUA DAT.
    /// Dac ta chi noi "trangthaiDvXuly = 12"; chap nhan them bien the ghi vao
    /// <c>trangthaiXuly</c> tren ban ghi NGHIEMTHU vi §4.4 co ca hai cot.
    /// </summary>
    public static bool LaBanGhiTraLai(XuLyNhiemVu? x)
    {
        if (x is null) return false;
        if (x.TrangThaiDvXuly == TrangThaiPh.TuChoi) return true;
        return x.TrangThaiDvXuly is null
               && string.Equals(x.Loai, LoaiXuLy.NghiemThu, StringComparison.Ordinal)
               && x.TrangThaiXuLy == TrangThaiPh.TuChoi;
    }

    /// <summary>
    /// §9.3 dieu 2 — <c>vaitro</c> co chua NGUOI_THUC_HIEN khong.
    /// Dung <c>Contains</c> de van dung khi mot tai khoan giu nhieu vai (chuoi ghep).
    /// </summary>
    public static bool CoVaiTroThucHien(SysUser? u) =>
        u is not null
        && !string.IsNullOrWhiteSpace(u.VaiTro)
        && u.VaiTro.Contains(VaiTro.NguoiThucHien, StringComparison.Ordinal);

    /// <summary>§9.3 dieu 1 + 2 — ung vien hop le o muc co ban (chua xet pham vi don vi).</summary>
    public static bool LaUngVienHopLe(SysUser? u) =>
        u is not null && u.TrangThai == 1 && CoVaiTroThucHien(u);

    // ======================================================================
    // 2. §9.4 — CHAM 5 DAC TRUNG
    // ======================================================================

    /// <summary>
    /// §9.4 — tinh S1..S5 cho mot ung vien. Ham THUAN TUY, khong sua <paramref name="db"/>.
    ///
    /// DIEN GIAI DA CHOT (ghi ro trong bao cao): CA NAM dac trung deu tinh tren cac nhiem vu
    /// nguoi do giu vai CHUTRI. §9.4 chi noi ro "vaitro = 'CHUTRI'" o S1 va S4; S2/S3/S5 viet
    /// chung chung. Chon CHUTRI cho ca 5 de nhat quan voi view <c>USER_TAI_HIENTAI</c> (§4.8)
    /// va vi nguoi PHOIHOP khong chiu trach nhiem chinh ve ket qua — quy cong hay quy loi cho
    /// ho deu lech ban chat. He qua phai chap nhan: nguoi chi tung tham gia voi vai PHOIHOP
    /// se duoc cham nhu nguoi moi.
    /// </summary>
    public static DacTrungUngVien TinhDacTrung(
        AiSnapshot db,
        SysUser user,
        GoiYRequest? req,
        CauHinhAiDto? cauHinh = null,
        ChiMucAi? chiMuc = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(user);

        var c = cauHinh ?? CauHinhAi.HienTai;
        var cm = chiMuc ?? ChiMucAi.Xay(db);
        req ??= new GoiYRequest();

        var maLv = string.IsNullOrWhiteSpace(req.LinhVuc) ? null : req.LinhVuc!.Trim();
        var khongCoLinhVuc = maLv is null;

        DmLinhVuc? lvHienTai = null;
        if (maLv is not null) cm.LinhVucTheoMa.TryGetValue(maLv, out lvHienTai);

        var nhomCha = lvHienTai?.NhomCha;
        if (string.IsNullOrWhiteSpace(nhomCha)) nhomCha = null;

        List<DmNhiemVuChiTiet> dsChuTri =
            cm.ChuTriTheoUser.TryGetValue(user.Id, out var tmp) ? tmp : new List<DmNhiemVuChiTiet>();

        // --- Bien dem ---
        var n = 0;              // §9.4 S1 — n
        var nPhu = 0;           // §9.4 S1 — n'
        var soHoanThanh = 0;    // §9.4 S2 — N
        var soDungHan = 0;      // §9.4 S3(a)
        var soTraLai = 0;       // §9.4 S3(b)
        var soLanGiaHan = 0;    // §9.4 S3(c)
        var soNvDangMo = 0;     // §4.8 view
        var taiTrongSo = 0.0;   // §9.4 S4
        var q = 0;              // §9.4 S5

        foreach (var nv in dsChuTri)
        {
            if (nv is null) continue;

            if (DaNghiemThu(nv))
            {
                soHoanThanh++;                                                  // §9.4 S2
                if (nv.TrangThai == TrangThaiNv.HoanThanh) soDungHan++;         // §9.4 S3(a)

                if (maLv is not null)
                {
                    if (string.Equals(nv.LinhVuc, maLv, StringComparison.OrdinalIgnoreCase))
                    {
                        n++;                                                    // §9.4 S1 (n)
                    }
                    else if (nhomCha is not null
                             && !string.IsNullOrWhiteSpace(nv.LinhVuc)
                             && cm.LinhVucTheoMa.TryGetValue(nv.LinhVuc!, out var lvCuaNv)
                             && string.Equals(lvCuaNv.NhomCha, nhomCha, StringComparison.OrdinalIgnoreCase))
                    {
                        nPhu++;                                                 // §9.4 S1 (n')
                    }
                }
            }

            if (DangGiuViec(nv))                                                // §9.4 S4 / §4.8 view
            {
                soNvDangMo++;
                taiTrongSo += CauHinhAi.TrongSoDoKhan(nv.DoKhan);
                if (nv.TrangThai == TrangThaiNv.DangTrienKhaiQuaHan) q++;       // §9.4 S5
            }

            soLanGiaHan += nv.SoLanGiaHan;                                      // §9.4 S3(c)
            soTraLai += cm.SoTraLaiTheoNv.TryGetValue(nv.Id, out var st) ? st : 0; // §9.4 S3(b)
        }

        // --- S1 Chuyen mon (§9.4 S1) — suy tu lich su, khong can khai bao (§10.1) ---
        var nguongCM = c.NguongChuyenMon > 0 ? c.NguongChuyenMon : 1;
        double s1;
        int nDungChoS1;
        var dungChietKhau = false;
        if (khongCoLinhVuc)
        {
            // §9.5 "Nhiem vu khong co linh vuc": bo rang buoc linh vuc, tinh tren TONG N
            nDungChoS1 = soHoanThanh;
            s1 = Math.Min(1.0, soHoanThanh / nguongCM);
        }
        else if (n > 0)
        {
            nDungChoS1 = n;
            s1 = Math.Min(1.0, n / nguongCM);
        }
        else
        {
            nDungChoS1 = 0;
            dungChietKhau = true;
            s1 = CauHinhAi.ChietKhauNhomCha * Math.Min(1.0, nPhu / nguongCM);
        }
        s1 = SoHoc.Keo(s1, 0, 1);

        // --- S2 Lich su thuc hien (§9.4 S2): ln(1+N) / ln(1+nguong), cat nguong tai 1 ---
        var nguongLS = c.NguongLichSu > 0 ? c.NguongLichSu : 10;
        var mauLog = Math.Log(1 + nguongLS);
        var s2 = mauLog > 0 ? Math.Min(1.0, Math.Log(1 + soHoanThanh) / mauLog) : (soHoanThanh > 0 ? 1.0 : 0.0);
        s2 = SoHoc.Keo(s2, 0, 1);

        // --- S3 Hieu qua cong viec (§9.4 S3) ---
        // KHONG dua hsChatluong vao cong thuc (§9.4 ghi chu + §10.2).
        // Luu y mau thuan noi tai cua dac ta: §9.5 dong 1 ghi nguoi moi co "S3 ~ 0,655" vi con
        // cong r_chatluong = 0,60 — nhung chinh §9.4 va §10.2 da LOAI hsChatluong. Ban nay theo
        // §9.4 / §10.2 nen nguoi moi ra dung S3 = p0 = 0,70. §9.5 can duoc dinh chinh.
        var mauR = soHoanThanh + c.Alpha;
        var rDungHan = mauR > 0 ? (soDungHan + c.Alpha * c.P0) / mauR : c.P0;
        var tyLeTraLai = soTraLai / Math.Max(1.0, soHoanThanh);       // max(1, ...) chan chia 0
        var tyLeGiaHan = soLanGiaHan / Math.Max(1.0, soHoanThanh);    // max(1, ...) chan chia 0
        var s3 = SoHoc.Keo(rDungHan - c.PhatTraLai * tyLeTraLai - c.PhatGiaHan * tyLeGiaHan, 0, 1);

        // --- S4 Khoi luong hien tai (§9.4 S4) ---
        var k = user.MaxConcurrentTasks > 0 ? user.MaxConcurrentTasks : GioiHan.MaxConcurrentTasksMacDinh;
        var s4 = SoHoc.Keo(1 - taiTrongSo / k, 0, 1);

        // --- S5 Tinh san sang (§9.4 S5) ---
        var s5 = SoHoc.Keo(1 - Math.Min(1.0, (double)q / CauHinhAi.NguongQuaHanS5), 0, 1);

        DmLinhVuc? lvCha = null;
        if (nhomCha is not null) cm.LinhVucTheoMa.TryGetValue(nhomCha, out lvCha);

        return new DacTrungUngVien
        {
            S1 = s1,
            S2 = s2,
            S3 = s3,
            S4 = s4,
            S5 = s5,
            SoLieu = new SoLieuDto
            {
                SoNvHoanThanhLinhVuc = n,
                SoNvHoanThanh = soHoanThanh,
                SoNvDungHan = soDungHan,
                SoNvBiTraLai = soTraLai,
                SoLanGiaHan = soLanGiaHan,
                SoNvDangMo = soNvDangMo,
                TaiTrongSo = SoHoc.LamTron(taiTrongSo, 2),
                SoNvQuaHan = q,
                K = k,
                TyLeDungHan = soHoanThanh > 0
                    ? SoHoc.LamTron(soDungHan / (double)soHoanThanh, 4)
                    : 0,
                // Hai khoa duoi LUON null o v1 (§9.2 / §10.1 bo bang khai bao nang luc;
                // §9.4 ghi chu + §10.2 loai hsChatluong). Giu khoa de FE khong in "undefined".
                MucThanhThao = null,
                DiemChatLuongTb = null,
                SoNvDuocGiao = dsChuTri.Count
            },
            CoSoTinh = new CoSoTinhAi
            {
                MaLinhVuc = maLv,
                TenLinhVuc = lvHienTai?.Ten ?? maLv,
                KhongCoLinhVuc = khongCoLinhVuc,
                MaNhomCha = nhomCha,
                TenNhomCha = lvCha?.Ten ?? nhomCha,
                SoNvLinhVuc = n,
                SoNvNhomCha = nPhu,
                SoNvDungChoS1 = nDungChoS1,
                DungChietKhauNhomCha = dungChietKhau,
                SoNvHoanThanhTong = soHoanThanh,
                RDungHan = SoHoc.LamTron(rDungHan, 4),
                TyLeTraLai = SoHoc.LamTron(tyLeTraLai, 4),
                TyLeGiaHan = SoHoc.LamTron(tyLeGiaHan, 4),
                Tai = SoHoc.LamTron(taiTrongSo, 2),
                K = k,
                SoNvQuaHan = q,
                NguongChuyenMon = nguongCM,
                NguongLichSu = nguongLS,
                CoDuLieuHieuQua = soHoanThanh > 0
            }
        };
    }

    // ======================================================================
    // 3. §9.3 — LOC CUNG (chay TRUOC khi cham diem)
    // ======================================================================

    /// <summary>
    /// §9.3 — sau dieu kien loai tru. LUU Y dieu 6: nguoi qua tai KHONG bi loai,
    /// chi bi gan nhan do "Qua tai" va bi day xuong cuoi danh sach.
    /// </summary>
    public static KetQuaLocCung LocCung(
        AiSnapshot db,
        GoiYRequest? req,
        CauHinhAiDto? cauHinh = null,
        ChiMucAi? chiMuc = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        var c = cauHinh ?? CauHinhAi.HienTai;
        var cm = chiMuc ?? ChiMucAi.Xay(db);
        req ??= new GoiYRequest();

        var kq = new KetQuaLocCung();

        var phamVi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ma in req.PhamViUnitCode)
        {
            if (!string.IsNullOrWhiteSpace(ma)) phamVi.Add(ma.Trim());
        }

        var loaiTru = new HashSet<Guid>();
        foreach (var id in req.LoaiTru)
        {
            if (id != Guid.Empty) loaiTru.Add(id);
        }

        // §9.3 dieu 4 — nguoi da co ten trong phan cong cua CHINH nhiem vu nay.
        // Nhiem vu dang TAO MOI chua co ma, khi do caller truyen daPhanCong[] thay the.
        var daPhanCong = new HashSet<Guid>();
        if (req.IdNvChiTiet.HasValue
            && cm.PhanCongTheoNv.TryGetValue(req.IdNvChiTiet.Value, out var dsPc))
        {
            foreach (var p in dsPc) daPhanCong.Add(p.UserId);
        }
        foreach (var id in req.DaPhanCong)
        {
            if (id != Guid.Empty) daPhanCong.Add(id);
        }

        // §9.3 dieu 5 — nguoi da tung tu choi chinh nhiem vu nay
        var daTuChoi = new HashSet<Guid>();
        if (req.IdNvChiTiet.HasValue
            && cm.TuChoiTheoNv.TryGetValue(req.IdNvChiTiet.Value, out var tapTc))
        {
            foreach (var id in tapTc) daTuChoi.Add(id);
        }
        foreach (var id in req.DaTuChoi)
        {
            if (id != Guid.Empty) daTuChoi.Add(id);
        }

        kq.CoLocTrungLap = req.IdNvChiTiet.HasValue || daPhanCong.Count > 0 || daTuChoi.Count > 0;

        foreach (var u in db.NguoiDung)
        {
            if (u is null) continue;

            // §9.3 dieu 1 — tai khoan bi khoa
            if (u.TrangThai != 1)
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Tài khoản đã bị khoá"));
                continue;
            }

            // §9.3 dieu 2 — khong co vai tro Nguoi thuc hien
            if (!CoVaiTroThucHien(u))
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Không có vai trò Người thực hiện"));
                continue;
            }

            // §9.3 dieu 3 — ngoai pham vi don vi (rong = khong gioi han)
            if (phamVi.Count > 0 && !phamVi.Contains(u.UnitCode ?? string.Empty))
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Ngoài phạm vi đơn vị được chọn"));
                continue;
            }

            // §9.3 dieu 4 — da duoc phan cong trong chinh nhiem vu nay
            if (daPhanCong.Contains(u.Id))
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Đã được phân công trong nhiệm vụ này"));
                continue;
            }

            // (cung dieu 4) danh sach loai tru do nguoi giao truyen len
            if (loaiTru.Contains(u.Id))
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Đã được người giao chọn hoặc loại trừ"));
                continue;
            }

            // §9.3 dieu 5 — da tung tu choi nhiem vu nay
            if (daTuChoi.Contains(u.Id))
            {
                kq.LoaiBo.Add(TaoBanGhiLoai(u, "Đã từ chối nhiệm vụ này trước đó"));
                continue;
            }

            // §9.3 dieu 6 — qua tai nghiem trong: VAN HIEN THI, chi gan nhan + day xuong cuoi
            var dt = TinhDacTrung(db, u, req, c, cm);
            kq.DacTrung[u.Id] = dt;
            var heSo = c.HeSoQuaTai > 0 ? c.HeSoQuaTai : 1.5;
            if (dt.SoLieu.TaiTrongSo >= dt.SoLieu.K * heSo) kq.QuaTai.Add(u.Id);

            kq.UngVien.Add(u);
        }

        return kq;
    }

    private static UngVienBiLoaiDto TaoBanGhiLoai(SysUser u, string lyDo) => new()
    {
        UserId = u.Id,
        FullName = u.FullName,
        LyDo = lyDo
    };

    // ======================================================================
    // 4. §9.5 / §9.6 / §9.7 — NHAN HIEN THI
    // ======================================================================

    /// <summary>§9.5 / §9.7 — cac nhan canh bao gan kem ung vien.</summary>
    public static List<NhanDto> TaoNhan(DacTrungUngVien dt, double doTinCay, bool laQuaTai)
    {
        ArgumentNullException.ThrowIfNull(dt);
        var ds = new List<NhanDto>();

        // §9.5 dong 1 — nguoi dung moi, chua tung duoc giao viec nao
        if (dt.SoLieu.SoNvDuocGiao == 0)
        {
            ds.Add(new NhanDto(MaNhanUngVien.NguoiMoi, "Người mới – chưa có dữ liệu lịch sử", "xam"));
        }

        // §9.4 "He so tin cay" — duoi 0,4 thi hien nhan "Du lieu con it"
        if (doTinCay < CauHinhAi.NguongDoTinCayThap)
        {
            ds.Add(new NhanDto(MaNhanUngVien.DuLieuIt, "Dữ liệu còn ít", "vang"));
        }

        // §9.3 dieu 6
        if (laQuaTai)
        {
            ds.Add(new NhanDto(MaNhanUngVien.QuaTai, "⚠ Quá tải", "do"));
        }

        // §9.4 S5 — dang co viec qua han
        if (dt.SoLieu.SoNvQuaHan > 0)
        {
            ds.Add(new NhanDto(MaNhanUngVien.CoQuaHan, "⚠ Đang có nhiệm vụ quá hạn", "cam"));
        }

        // §9.4 S3(b) — chi gan nhan khi ty le tra lai THAT SU cao (§9.6 ghi "bi tra lai NHIEU").
        var soHt = dt.SoLieu.SoNvHoanThanh;
        var tyLeTraLai = dt.SoLieu.SoNvBiTraLai / (soHt > 0 ? soHt : 1.0);
        if (dt.SoLieu.SoNvBiTraLai > 0 && tyLeTraLai >= CauHinhAi.NguongTyLeTraLaiGanNhan)
        {
            ds.Add(new NhanDto(MaNhanUngVien.BiTraLai,
                "⚠ Hay bị trả lại (" + dt.SoLieu.SoNvBiTraLai + " lần)", "cam"));
        }

        return ds;
    }

    // ======================================================================
    // 5. §9.6 H1 — SINH DANH SACH GOI Y (ham THUAN TUY)
    // ======================================================================

    /// <summary>
    /// §9.6 H1 — cham diem va xep hang ung vien. Ham THUAN TUY: khong doc/ghi CSDL,
    /// khong dung dong ho he thong ngoai <see cref="AiSnapshot.HomNay"/>, khong dung so ngau nhien.
    ///
    /// Thu tu: loc cung §9.3 -&gt; cham S1..S5 §9.4 -&gt; xac dinh che do §9.5 -&gt; nhan + ly do §9.6
    /// -&gt; sap xep (QUA_TAI luon cuoi) -&gt; cat theo <c>soLuong</c>.
    /// </summary>
    public static GoiYResponse GoiY(AiSnapshot db, GoiYRequest? req, CauHinhAiDto? cauHinh = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        var c = cauHinh ?? CauHinhAi.HienTai;
        req ??= new GoiYRequest();

        var cm = ChiMucAi.Xay(db);
        var loc = LocCung(db, req, c, cm);

        // §9.5 dong cuoi — toan he thong chua co nhiem vu nao duoc nghiem thu
        var cheDo = cm.HeThongCoNghiemThu ? CheDoGoiY.DayDu : CheDoGoiY.KhoiTao;

        var canhBao = new List<string>();
        if (string.Equals(cheDo, CheDoGoiY.KhoiTao, StringComparison.Ordinal))
        {
            canhBao.Add("Chế độ khởi tạo: gợi ý dựa trên chuyên môn và khối lượng hiện tại.");
        }
        if (string.IsNullOrWhiteSpace(req.LinhVuc))
        {
            canhBao.Add("Nhiệm vụ chưa gán lĩnh vực — độ chính xác gợi ý giảm.");
        }

        var dong = new List<DongXepHang>();
        foreach (var u in loc.UngVien)
        {
            var dt = loc.DacTrung.TryGetValue(u.Id, out var daCham)
                ? daCham
                : TinhDacTrung(db, u, req, c, cm);
            var laQuaTai = loc.QuaTai.Contains(u.Id);

            // §9.5 dong cuoi: che do KHOI_TAO chi dung S1 va S4 voi w = 0,6 / 0,4
            double w1, w2, w3, w4, w5;
            if (string.Equals(cheDo, CheDoGoiY.KhoiTao, StringComparison.Ordinal))
            {
                w1 = CauHinhAi.TrongSoKhoiTaoS1;
                w2 = 0;
                w3 = 0;
                w4 = CauHinhAi.TrongSoKhoiTaoS4;
                w5 = 0;
            }
            else
            {
                w1 = c.W1;
                w2 = c.W2;
                w3 = c.W3;
                w4 = c.W4;
                w5 = c.W5;
            }

            // §9.4 "Diem tong": DIEM = 100 x (w1.S1 + ... + w5.S5)
            // §9.1 "giai thich duoc" + vi du §9.6: gopPhan phai tinh tu CHINH con so `diem`
            // DA LAM TRON 2 chu so, va tong 5 gopPhan phai bang diemTong — neu khong, bang
            // phan ra hien tren M06 cong lai khong ra tong (30,0+17,2+22,0+15,0+3,2 = 87,4).
            var tp = new DiemThanhPhanDto
            {
                ChuyenMon = TaoThanhPhan(dt.S1, w1),
                LichSu = TaoThanhPhan(dt.S2, w2),
                HieuQua = TaoThanhPhan(dt.S3, w3),
                KhoiLuong = TaoThanhPhan(dt.S4, w4),
                SanSang = TaoThanhPhan(dt.S5, w5)
            };
            var diemTong = SoHoc.LamTron(tp.TongGopPhan, 1);

            // §9.4 "He so tin cay" — khong nhan vao diem, chi hien thi.
            // DIEN GIAI: dac ta viet `co_ho_so_chuyen_mon`, nhung ban nay khong co bang khai bao
            // nang luc (§9.2 / §10.1) nen hieu la "da tung hoan thanh it nhat 1 nhiem vu linh vuc L".
            var coHoSoChuyenMon = dt.CoSoTinh.KhongCoLinhVuc
                ? dt.CoSoTinh.SoNvHoanThanhTong > 0
                : dt.CoSoTinh.SoNvLinhVuc > 0;
            var doTinCay = SoHoc.Keo(
                dt.SoLieu.SoNvHoanThanh / CauHinhAi.ChiaDoTinCay * CauHinhAi.HeSoDoTinCayLichSu
                + (coHoSoChuyenMon ? CauHinhAi.CongDoTinCayCoChuyenMon : 0),
                0, 1);
            doTinCay = SoHoc.LamTron(doTinCay, 2);

            var nhan = TaoNhan(dt, doTinCay, laQuaTai);
            cm.DonViTheoMa.TryGetValue(u.UnitCode ?? string.Empty, out var donVi);

            var item = new UngVienDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                ChucVu = u.ChucVu ?? string.Empty,
                UnitName = donVi?.TenDonVi ?? u.UnitCode ?? string.Empty,
                DiemTong = diemTong,
                DoTinCay = doTinCay,
                Nhan = nhan,
                NhanText = nhan.Select(x => x.Nhan).ToList(),
                DiemThanhPhan = tp,
                LyDo = SinhLyDo.Sinh(dt).ToList(),
                SoLieu = dt.SoLieu
            };

            dong.Add(new DongXepHang(item, laQuaTai, dt.S4));
        }

        // §9.5 "Nguyen tac": khong bao gio tra danh sach rong.
        // Neu MOI ung vien deu 0 diem thi xep theo S4 (ai ranh nhat) va noi ro ly do.
        var deu0 = dong.Count > 0 && dong.All(x => x.Item.DiemTong <= 0);
        if (deu0)
        {
            canhBao.Add("Mọi ứng viên đều chưa có dữ liệu để chấm điểm — danh sách được xếp theo mức độ rảnh (khối lượng hiện tại).");
        }
        if (dong.Count == 0)
        {
            // Khong the "sinh" nguoi khi bo loc cung §9.3 da loai het.
            canhBao.Add("Không có ứng viên nào thoả điều kiện lọc — hãy mở rộng phạm vi đơn vị hoặc chọn thủ công.");
        }

        // §9.3 dieu 6 + §9.7: nguoi QUA_TAI luon xep SAU moi nguoi khong qua tai.
        // Cac khoa phu (S4, ho ten, id) bao dam ket qua TAI LAP DUOC, khong phu thuoc
        // thu tu ban ghi tra ve tu CSDL.
        dong.Sort((a, b) => SoSanhDong(a, b, deu0));

        var soLuong = req.SoLuong > 0 ? req.SoLuong : GioiHan.SoUngVienMacDinh;
        var ketQua = new List<UngVienDto>();
        foreach (var x in dong)
        {
            if (ketQua.Count >= soLuong) break;
            ketQua.Add(x.Item);
        }

        return new GoiYResponse
        {
            GoiYId = Guid.NewGuid(),
            PhienBanTrongSo = c.PhienBan,
            CheDo = cheDo,
            CanhBao = canhBao,
            UngVien = ketQua,
            IdNvChiTiet = req.IdNvChiTiet,
            LinhVuc = string.IsNullOrWhiteSpace(req.LinhVuc) ? null : req.LinhVuc,
            TongSoUngVien = dong.Count,
            DaLocTrungLap = loc.CoLocTrungLap,
            LoaiBo = loc.LoaiBo
        };
    }

    private static ThanhPhanDto TaoThanhPhan(double diem, double trongSo)
    {
        var diemLamTron = SoHoc.LamTron(diem, 2);
        return new ThanhPhanDto(diemLamTron, trongSo, SoHoc.LamTron(100 * trongSo * diemLamTron, 1));
    }

    private static int SoSanhDong(DongXepHang a, DongXepHang b, bool deu0)
    {
        // 1) Nguoi qua tai luon xep sau (§9.3 dieu 6)
        if (a.QuaTai != b.QuaTai) return a.QuaTai ? 1 : -1;

        if (deu0)
        {
            // 2a) Moi nguoi deu 0 diem -> ai ranh nhat len truoc (§9.5 "Nguyen tac")
            var soSanhS4 = b.S4.CompareTo(a.S4);
            if (soSanhS4 != 0) return soSanhS4;
        }
        else
        {
            // 2b) Diem giam dan
            var soSanhDiem = b.Item.DiemTong.CompareTo(a.Item.DiemTong);
            if (soSanhDiem != 0) return soSanhDiem;

            // 3) Hoa diem -> ai ranh hon len truoc
            var soSanhS4 = b.S4.CompareTo(a.S4);
            if (soSanhS4 != 0) return soSanhS4;
        }

        // 4) Van hoa -> theo ho ten roi theo id, de ket qua tai lap duoc.
        // Dung so sanh ORDINAL (khong theo culture) de may nao chay cung ra mot thu tu.
        var soSanhTen = string.CompareOrdinal(a.Item.FullName ?? string.Empty, b.Item.FullName ?? string.Empty);
        if (soSanhTen != 0) return soSanhTen;
        return a.Item.UserId.CompareTo(b.Item.UserId);
    }

    /// <summary>Mot dong trong bang xep hang tam (khong tra ra ngoai).</summary>
    private sealed class DongXepHang
    {
        public DongXepHang(UngVienDto item, bool quaTai, double s4)
        {
            Item = item;
            QuaTai = quaTai;
            S4 = s4;
        }

        public UngVienDto Item { get; }

        public bool QuaTai { get; }

        public double S4 { get; }
    }

    // ======================================================================
    // 6. §4.8 AI_GOIY_LOG — doc / ghi ket_qua_json
    // ======================================================================

    /// <summary>§4.8 — dung chuoi <c>ket_qua_json</c> tu danh sach ung vien da xep hang.</summary>
    public static string SinhKetQuaJson(IReadOnlyList<UngVienDto> ungVien)
    {
        ArgumentNullException.ThrowIfNull(ungVien);
        var ds = new List<UngVienNhatKyJson>(ungVien.Count);
        for (var i = 0; i < ungVien.Count; i++)
        {
            var uv = ungVien[i];
            ds.Add(new UngVienNhatKyJson
            {
                ThuHang = i + 1,
                UserId = uv.UserId,
                FullName = uv.FullName,
                DiemTong = uv.DiemTong,
                DoTinCay = uv.DoTinCay,
                DiemThanhPhan = uv.DiemThanhPhan,
                Nhan = uv.Nhan.Select(x => x.Ma).ToList()
            });
        }
        return JsonSerializer.Serialize(ds, TuyChonJson);
    }

    /// <summary>
    /// §4.8 — doc lai <c>ket_qua_json</c>. Ban ghi hong (JSON sai dinh dang) tra ve danh sach
    /// rong thay vi lam do ca API: nhat ky la du lieu do luong, khong duoc chan nghiep vu.
    /// </summary>
    public static IReadOnlyList<UngVienNhatKyJson> DocKetQuaJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<UngVienNhatKyJson>();
        try
        {
            var ds = JsonSerializer.Deserialize<List<UngVienNhatKyJson>>(json!, TuyChonJson);
            return ds ?? (IReadOnlyList<UngVienNhatKyJson>)Array.Empty<UngVienNhatKyJson>();
        }
        catch (JsonException)
        {
            return Array.Empty<UngVienNhatKyJson>();
        }
    }

    // ======================================================================
    // 7. CAI DAT IRecommendationService
    // ======================================================================

    /// <inheritdoc />
    public async Task<GoiYResponse> GoiYAsync(GoiYRequest req, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);

        var cauHinh = await DocCauHinhAsync(ct).ConfigureAwait(false);
        var db = await _nguon.LaySnapshotAsync(false, ct).ConfigureAwait(false);

        var kq = GoiY(db, req, cauHinh);

        // §4.8 — ghi nhat ky NGAY khi tra goi y; H2 se cap nhat nguoi duoc chon sau.
        var banGhi = new AiGoiYLog
        {
            Id = kq.GoiYId,
            IdNvChiTiet = req.IdNvChiTiet,
            LinhVuc = string.IsNullOrWhiteSpace(req.LinhVuc) ? null : req.LinhVuc,
            KetQuaJson = SinhKetQuaJson(kq.UngVien),
            UserIdDaChon = null,
            ThuHangDaChon = null,
            CoTrongGoiY = false,
            PhienBanTrongSo = kq.PhienBanTrongSo,
            CheDo = kq.CheDo,
            SoUngVien = kq.UngVien.Count,
            UserIdGoiY = _hienTai?.UserId,
            CreateDate = DateTime.Now
        };
        await _nguon.ThemNhatKyAsync(banGhi, ct).ConfigureAwait(false);

        return kq;
    }

    /// <inheritdoc />
    public async Task GhiKetQuaAsync(Guid goiYId, Guid? userIdDaChon, CancellationToken ct)
    {
        var banGhi = await _nguon.LayNhatKyAsync(goiYId, ct).ConfigureAwait(false);
        if (banGhi is null)
        {
            throw new KeyNotFoundException("Không tìm thấy bản ghi nhật ký gợi ý cần cập nhật.");
        }

        // §4.8 thu_hang_da_chon — 1-based, null khi nguoi duoc chon khong nam trong goi y
        int? thuHang = null;
        if (userIdDaChon.HasValue && userIdDaChon.Value != Guid.Empty)
        {
            foreach (var uv in DocKetQuaJson(banGhi.KetQuaJson))
            {
                if (uv.UserId == userIdDaChon.Value)
                {
                    thuHang = uv.ThuHang;
                    break;
                }
            }
        }

        banGhi.UserIdDaChon = userIdDaChon.HasValue && userIdDaChon.Value != Guid.Empty
            ? userIdDaChon
            : null;
        banGhi.ThuHangDaChon = thuHang;

        // §4.8: co_trong_goi_y CHOT CUNG o top-5, KHONG phu thuoc soLuong cua request —
        // co vay §9.8 moi so sanh duoc giua cac lan goi (xem ghi chu o IRecommendationService).
        banGhi.CoTrongGoiY = thuHang.HasValue && thuHang.Value <= CauHinhAi.TopChotNhatKy;

        await _nguon.CapNhatNhatKyAsync(banGhi, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CauHinhAiDto> DocCauHinhAsync(CancellationToken ct)
    {
        var daLuu = await _nguon.DocCauHinhAsync(ct).ConfigureAwait(false);
        if (daLuu is not null && KiemTraCauHinh(daLuu).HopLe)
        {
            CauHinhAi.Dat(daLuu);
        }
        return CauHinhAi.HienTai;
    }

    /// <inheritdoc />
    public async Task<Result<CauHinhAiDto>> LuuCauHinhAsync(CauHinhAiDto cauHinh, CancellationToken ct)
    {
        if (cauHinh is null)
        {
            return Result<CauHinhAiDto>.ThatBai("Thiếu dữ liệu cấu hình.", MaLoiChung.DuLieuKhongHopLe);
        }

        var kiemTra = KiemTraCauHinh(cauHinh);
        if (!kiemTra.HopLe)
        {
            return Result<CauHinhAiDto>.ThatBai(
                string.Join(" ", kiemTra.CanhBao), MaLoiChung.DuLieuKhongHopLe);
        }

        CauHinhAi.Dat(cauHinh);
        await _nguon.LuuCauHinhAsync(CauHinhAi.HienTai, ct).ConfigureAwait(false);
        return Result<CauHinhAiDto>.Ok(CauHinhAi.HienTai);
    }

    /// <inheritdoc />
    public KiemTraCauHinhAiDto KiemTraCauHinh(CauHinhAiDto cauHinh) => CauHinhAi.KiemTra(cauHinh);
}
