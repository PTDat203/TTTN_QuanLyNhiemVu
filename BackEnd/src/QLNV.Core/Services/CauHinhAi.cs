using System.Globalization;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Core.Services;

/// <summary>
/// Tien ich so hoc dung chung cho dong co goi y (§9.4) va cho do hieu qua (§9.8).
///
/// Ban JS goc dung <c>Math.round(x * he) / he</c> — tuc lam tron NUA LEN.
/// C# mac dinh lam tron "ngan hang" (0,5 ve so chan) nen o day phai chi dinh
/// <see cref="MidpointRounding.AwayFromZero"/>, neu khong cac vi du cua §9.6
/// (30,0 + 17,2 + 22,0 + 15,0 + 3,2 = 87,4) se lech.
/// </summary>
public static class SoHoc
{
    /// <summary>Lam tron nua len; gia tri khong hop le (NaN / vo cuc) tra 0.</summary>
    public static double LamTron(double x, int soChuSo)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) return 0;
        if (soChuSo < 0) soChuSo = 0;
        if (soChuSo > 15) soChuSo = 15;
        return Math.Round(x, soChuSo, MidpointRounding.AwayFromZero);
    }

    /// <summary>Keo gia tri ve doan [thap, cao]; NaN tra <paramref name="thap"/>.</summary>
    public static double Keo(double x, double thap, double cao)
    {
        if (double.IsNaN(x)) return thap;
        if (x < thap) return thap;
        return x > cao ? cao : x;
    }

    /// <summary>Chia co chan mau 0 — moi cong thuc ty le trong §9.4 / §9.8 deu di qua day.</summary>
    public static double ChiaAnToan(double tu, double mau)
    {
        if (mau == 0) return 0;
        var kq = tu / mau;
        return double.IsNaN(kq) || double.IsInfinity(kq) ? 0 : kq;
    }

    /// <summary>
    /// Dinh dang so kieu Viet Nam (dau phay thap phan) — dung trong cau ly do §9.6,
    /// vi du "tai 2,0/8". Khong phu thuoc culture cua may chay.
    /// </summary>
    public static string SoVN(double x, int soChuSo)
    {
        var dinhDang = "F" + soChuSo.ToString(CultureInfo.InvariantCulture);
        var chuoi = LamTron(x, soChuSo).ToString(dinhDang, CultureInfo.InvariantCulture);
        return chuoi.Replace(".", ",", StringComparison.Ordinal);
    }

    /// <summary>Lam tron ve so nguyen gan nhat (nua len) — dung cho phan tram trong cau ly do.</summary>
    public static int LamTronNguyen(double x)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) return 0;
        return (int)Math.Round(x, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// §5.8 H4 / §9.2 — bo trong so va hang so cua mo hinh cham diem.
///
/// La SINGLETON SUA DUOC LUC CHAY: man H4 goi <see cref="Dat"/> de doi trong so
/// ma khong phai khoi dong lai ung dung. Moi lan doc <see cref="HienTai"/> tra ve
/// mot BAN SAO, nen khong ai sua nham trang thai dung chung.
/// </summary>
public static class CauHinhAi
{
    private static readonly object Khoa = new();
    private static CauHinhAiDto _hienTai = new();

    // --- Hang so KHONG nam trong bo cau hinh (dac ta chot cung) ---

    /// <summary>§9.4 S5 — mau so co dinh 3 trong <c>S5 = 1 - min(1, q / 3)</c>.</summary>
    public const int NguongQuaHanS5 = 3;

    /// <summary>§9.4 "He so tin cay" — hang so chia 10 trong <c>(so_nv_hoanthanh / 10) x 0,6</c>.</summary>
    public const double ChiaDoTinCay = 10.0;

    /// <summary>§9.4 "He so tin cay" — phan cong them khi da tung lam dung linh vuc.</summary>
    public const double CongDoTinCayCoChuyenMon = 0.4;

    /// <summary>§9.4 — he so nhan cho phan lich su cua do tin cay.</summary>
    public const double HeSoDoTinCayLichSu = 0.6;

    /// <summary>§9.5 dong cuoi — che do KHOI_TAO chi xep theo S1 (0,6) va S4 (0,4).</summary>
    public const double TrongSoKhoiTaoS1 = 0.6;

    /// <summary>§9.5 dong cuoi — trong so S4 trong che do KHOI_TAO.</summary>
    public const double TrongSoKhoiTaoS4 = 0.4;

    /// <summary>§9.4 S1 — chiet khau 50% khi chi lam linh vuc cung nhom cha.</summary>
    public const double ChietKhauNhomCha = 0.5;

    /// <summary>§9.4 — do tin cay duoi moc nay thi gan nhan "Du lieu con it".</summary>
    public const double NguongDoTinCayThap = 0.4;

    /// <summary>
    /// Nguong gan nhan BI_TRA_LAI. §9.6 ghi dieu kien la "bi tra lai NHIEU" chu khong
    /// phai "co bi tra lai"; duoi 25% thi hau nhu ai lam lau nam cung dinh, nhan mat
    /// y nghia canh bao. Quyet dinh nay giu tu ban tham chieu da kiem chung.
    /// </summary>
    public const double NguongTyLeTraLaiGanNhan = 0.25;

    /// <summary>§9.6 mau cau khoi luong — S4 &gt;= 0,5 la "con nhieu du dia".</summary>
    public const double NguongS4Ranh = 0.5;

    /// <summary>§9.6 mau cau khoi luong — S4 &lt; 0,25 la "kha ban".</summary>
    public const double NguongS4Ban = 0.25;

    /// <summary>
    /// §4.8 <c>co_trong_goi_y</c> — CHOT CUNG o top-5, KHONG phu thuoc <c>soLuong</c>
    /// cua request. Neu khong, bam "Xem them 5 nguoi" (soLuong = 10) se lam
    /// Precision@3 / ty le chap nhan / MRR cua §9.8 khong con so sanh duoc giua cac lan.
    /// </summary>
    public const int TopChotNhatKy = 5;

    /// <summary>Bo trong so dang hieu luc (ban sao — sua vao ban sao khong anh huong he thong).</summary>
    public static CauHinhAiDto HienTai
    {
        get
        {
            lock (Khoa)
            {
                return Sao(_hienTai);
            }
        }
    }

    /// <summary>Thay bo trong so dang hieu luc (§5.8 H4). Luu ban sao de tranh bi sua tu ben ngoai.</summary>
    public static void Dat(CauHinhAiDto cauHinh)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);
        lock (Khoa)
        {
            _hienTai = Sao(cauHinh);
        }
    }

    /// <summary>Khoi phuc bo mac dinh cua §9.2 (dung khi chua tung luu cau hinh, hoac khi kiem thu).</summary>
    public static void DatMacDinh()
    {
        lock (Khoa)
        {
            _hienTai = new CauHinhAiDto();
        }
    }

    /// <summary>Nhan ban mot bo cau hinh.</summary>
    public static CauHinhAiDto Sao(CauHinhAiDto nguon)
    {
        ArgumentNullException.ThrowIfNull(nguon);
        return new CauHinhAiDto
        {
            W1 = nguon.W1,
            W2 = nguon.W2,
            W3 = nguon.W3,
            W4 = nguon.W4,
            W5 = nguon.W5,
            NguongChuyenMon = nguon.NguongChuyenMon,
            NguongLichSu = nguon.NguongLichSu,
            P0 = nguon.P0,
            Alpha = nguon.Alpha,
            PhatTraLai = nguon.PhatTraLai,
            PhatGiaHan = nguon.PhatGiaHan,
            HeSoQuaTai = nguon.HeSoQuaTai,
            PhienBan = nguon.PhienBan
        };
    }

    /// <summary>
    /// §5.8 H4 — kiem tra tinh hop le cua bo cau hinh MA KHONG luu.
    /// Thong bao tra ve la tieng Viet co dau (yeu cau chung cua du an).
    /// </summary>
    public static KiemTraCauHinhAiDto KiemTra(CauHinhAiDto cauHinh)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);
        var canhBao = new List<string>();
        var tong = cauHinh.W1 + cauHinh.W2 + cauHinh.W3 + cauHinh.W4 + cauHinh.W5;

        // §9.2: w1 + ... + w5 = 1,00
        if (Math.Abs(tong - 1.0) > 1e-9)
        {
            canhBao.Add("Tổng trọng số đang là " + SoHoc.SoVN(tong, 2) + " — theo đặc tả phải bằng 1,00.");
        }

        if (cauHinh.W1 < 0 || cauHinh.W2 < 0 || cauHinh.W3 < 0 || cauHinh.W4 < 0 || cauHinh.W5 < 0)
        {
            canhBao.Add("Trọng số không được âm.");
        }

        // §9.4 S1 — mau so cua min(1, n / nguong)
        if (!(cauHinh.NguongChuyenMon > 0)) canhBao.Add("Ngưỡng chuyên môn phải lớn hơn 0.");

        // §9.4 S2 — mau so cua ln(1 + nguong)
        if (!(cauHinh.NguongLichSu > 0)) canhBao.Add("Ngưỡng lịch sử phải lớn hơn 0.");

        // §9.4 S3 — tien nghiem p0
        if (!(cauHinh.P0 >= 0 && cauHinh.P0 <= 1)) canhBao.Add("Tiên nghiệm p0 phải nằm trong khoảng 0–1.");

        // §9.4 S3 — so quan sat ao
        if (cauHinh.Alpha < 0) canhBao.Add("Hệ số làm mượt alpha không được âm.");

        if (cauHinh.PhatTraLai < 0) canhBao.Add("Hệ số phạt bị trả lại không được âm.");
        if (cauHinh.PhatGiaHan < 0) canhBao.Add("Hệ số phạt gia hạn không được âm.");

        // §9.3 dieu 6 — tai >= K x he so
        if (!(cauHinh.HeSoQuaTai > 0)) canhBao.Add("Hệ số quá tải phải lớn hơn 0.");

        if (string.IsNullOrWhiteSpace(cauHinh.PhienBan))
        {
            canhBao.Add("Phiên bản trọng số không được để trống.");
        }

        return new KiemTraCauHinhAiDto
        {
            TongTrongSo = SoHoc.LamTron(tong, 4),
            HopLe = canhBao.Count == 0,
            CanhBao = canhBao
        };
    }

    /// <summary>
    /// §9.4 S4 / §4.8 view <c>USER_TAI_HIENTAI</c> — trong so tai theo do khan.
    /// DOTXUAT = 2,0 · TRONGTAM = 1,5 · con lai = 1,0.
    /// </summary>
    public static double TrongSoDoKhan(string? doKhan) => DoKhan.TrongSo(doKhan);
}
