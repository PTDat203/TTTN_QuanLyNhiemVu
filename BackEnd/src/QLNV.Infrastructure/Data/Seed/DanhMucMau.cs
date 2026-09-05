using QLNV.Core.Constants;

namespace QLNV.Infrastructure.Data.Seed;

// =====================================================================================
// Cac ban ghi mo ta bo du lieu mau — port tu seed.js muc 3, 4, 5, 7, 8.
// Chung KHONG phai entity: day la "cong thuc" de BoSinhDuLieuMau dung ra entity that.
// =====================================================================================

/// <summary>§4.7 SYS_UNIT — mot don vi trong bo du lieu mau.</summary>
public sealed record DonViMau(string UnitCode, string TenDonVi, string? MaCha, int CapDonVi);

/// <summary>DM_LINHVUC — mot linh vuc (co the la nhom cha hoac linh vuc la).</summary>
public sealed record LinhVucMau(string Ma, string Ten, string? NhomCha);

/// <summary>§4.7 SYS_USER — mot nguoi dung mau. <c>Ma</c> la ma noi bo "U01".."U22".</summary>
public sealed record NguoiDungMau(
    string Ma,
    string UserName,
    string FullName,
    string ChucVu,
    string UnitCode,
    string VaiTro,
    int TrangThai,
    int MaxConcurrentTasks);

/// <summary>
/// Ho so sinh lich su cho mot nguoi thuc hien (seed.js muc 5).
/// <c>LinhVuc</c>: danh sach (ma linh vuc, so nhiem vu DA NGHIEM THU).
/// <c>DungHan</c>: ty le nhiem vu ket thuc voi truc A = 1 (con lai = 5).
/// <c>TraLai</c>: so nhiem vu tung bi cham CHUA DAT (§9.4 S3 phat tra lai).
/// <c>GiaHan1</c>/<c>GiaHan2</c>: so nhiem vu co solangiahan = 1 / = 2.
/// <c>ClMin</c>-<c>ClMax</c>: khoang hsChatluong khi nghiem thu.
/// </summary>
public sealed record HoSoLichSuMau(
    string Ma,
    IReadOnlyList<(string LinhVuc, int SoNv)> LinhVuc,
    double DungHan,
    int TraLai,
    int GiaHan1,
    int GiaHan2,
    int ClMin,
    int ClMax);

/// <summary>Van ban chi dao "noi bat" — chua cac nhiem vu dang song (seed.js muc 7).</summary>
public sealed record VanBanNoiBatMau(
    string Ma,
    string SoKyHieu,
    string LoaiVb,
    string LinhVuc,
    string CoQuan,
    string Nguon,
    string DoKhan,
    int LechNgayBanHanh,
    string TrichYeu);

/// <summary>Mot nhiem vu DANG SONG, phu cac o cua ma tran §2.4 (seed.js muc 8).</summary>
public sealed record NhiemVuSongMau(
    string Ma,
    string Vb,
    string LinhVuc,
    string ChuTri,
    string? ChuTriCu,
    IReadOnlyList<string> PhoiHop,
    string DoKhan,
    int TrangThai,
    int? TrangThaiDvXuly,
    int? TrangThaiXuLyGiaHan,
    int MucDoHt,
    int LechGiao,
    int SoNgay,
    string KichBan,
    string NoiDung);

/// <summary>Cac kich ban sinh chuoi lich su xu ly cho nhiem vu dang song.</summary>
public static class KichBanSong
{
    public const string ChuaTiepNhan = "CHUA_TIEP_NHAN";
    public const string DangLam = "DANG_LAM";
    public const string QuaHan = "QUA_HAN";
    public const string ChoXacNhan = "CHO_XAC_NHAN";
    public const string ChoXacNhanSauHan = "CHO_XAC_NHAN_SAU_HAN";
    public const string BiTraLai = "BI_TRA_LAI";
    public const string TuChoi = "TU_CHOI";
    public const string XinGiaHan = "XIN_GIA_HAN";
    public const string DaNghiemThu = "DA_NGHIEM_THU";
    public const string DaThuHoi = "DA_THU_HOI";
    public const string GiaHanBiTuChoi = "GIA_HAN_BI_TU_CHOI";
    public const string ThuHoiBaoCao = "THU_HOI_BAO_CAO";
}

/// <summary>
/// Toan bo danh muc tinh cua bo du lieu mau — port tu seed.js muc 3, 4, 5, 7, 8.
/// KHONG chua logic; chi la du lieu.
/// </summary>
public static class DanhMucMau
{
    /// <summary>Mat khau demo cho MOI tai khoan (§8 T9 — chi dung o moi truong demo).</summary>
    public const string MatKhauDemo = "123456";

    /// <summary>Ngay "hom nay" mac dinh cua bo du lieu mau.</summary>
    public static readonly DateOnly NgayMocMacDinh = new(2026, 9, 5);

    // ---------------------------------------------------------------------------------
    // §4.7 SYS_UNIT — 5 don vi, DV00 la cap 1 (cha cua tat ca)
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<DonViMau> DonVi = new[]
    {
        new DonViMau("DV00", "Sở Thông tin và Truyền thông", null, 1),
        new DonViMau("P01", "Phòng Công nghệ thông tin", "DV00", 2),
        new DonViMau("P02", "Phòng Bưu chính - Viễn thông", "DV00", 2),
        new DonViMau("P03", "Phòng Kế hoạch - Tài chính", "DV00", 2),
        new DonViMau("P04", "Văn phòng Sở", "DV00", 2)
    };

    // ---------------------------------------------------------------------------------
    // DM_LINHVUC — 3 nhom cha + 8 linh vuc la.
    // Phan cap phuc vu §9.4 S1: chua tung lam linh vuc L thi xet linh vuc CUNG NHOM CHA
    // roi chiet khau 50%.
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<LinhVucMau> LinhVuc = new[]
    {
        new LinhVucMau("CNTT", "Công nghệ thông tin", null),
        new LinhVucMau("HCTH", "Hành chính - Tổng hợp", null),
        new LinhVucMau("KHTC", "Kế hoạch - Tài chính", null),

        new LinhVucMau("CNTT_HT", "Hạ tầng - Hệ thống", "CNTT"),
        new LinhVucMau("CNTT_PM", "Phần mềm - Ứng dụng", "CNTT"),
        new LinhVucMau("CNTT_ATTT", "An toàn thông tin", "CNTT"),
        new LinhVucMau("CNTT_BCVT", "Bưu chính - Viễn thông", "CNTT"),

        new LinhVucMau("HC_VT", "Văn thư - Lưu trữ", "HCTH"),
        new LinhVucMau("HC_TCCB", "Tổ chức - Cán bộ", "HCTH"),

        new LinhVucMau("KH_DT", "Kế hoạch - Đầu tư", "KHTC"),
        new LinhVucMau("KH_TC", "Tài chính - Kế toán", "KHTC")
    };

    /// <summary>8 linh vuc LA — nhiem vu luon gan vao linh vuc la, khong gan vao nhom cha.</summary>
    public static readonly IReadOnlyList<string> LinhVucLa = new[]
    {
        "CNTT_HT", "CNTT_PM", "CNTT_ATTT", "CNTT_BCVT", "HC_VT", "HC_TCCB", "KH_DT", "KH_TC"
    };

    /// <summary>Phan bo do khan theo tan suat that: viec thuong xuyen nhieu nhat.</summary>
    public static readonly IReadOnlyList<string> DoKhanTheoTanSuat = new[]
    {
        DoKhan.ThuongXuyen, DoKhan.ThuongXuyen, DoKhan.ThuongXuyen, DoKhan.ThuongXuyen, DoKhan.ThuongXuyen,
        DoKhan.TrongTam, DoKhan.TrongTam, DoKhan.TrongTam, DoKhan.DotXuat, DoKhan.DotXuat
    };

    // ---------------------------------------------------------------------------------
    // §4.7 SYS_USER — 22 nguoi dung: 1 QUAN_TRI + 3 NGUOI_GIAO + 18 NGUOI_THUC_HIEN.
    // Cot chu thich danh dau CHAN DUNG (a)-(h) phuc vu demo AI.
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<NguoiDungMau> NguoiDung = new[]
    {
        // --- quan tri ---
        new NguoiDungMau("U01", "admin", "Nguyễn Quốc Hưng", "Quản trị hệ thống", "DV00", VaiTro.QuanTri, 1, 8),

        // --- nguoi giao (lanh dao) ---
        new NguoiDungMau("U02", "lehongphuc", "Lê Hồng Phúc", "Phó Giám đốc Sở", "DV00", VaiTro.NguoiGiao, 1, 8),
        new NguoiDungMau("U03", "tranminhhai", "Trần Minh Hải", "Trưởng phòng", "P01", VaiTro.NguoiGiao, 1, 8),
        new NguoiDungMau("U04", "phamthuha", "Phạm Thu Hà", "Trưởng phòng", "P03", VaiTro.NguoiGiao, 1, 8),

        // --- CHAN DUNG (a): chuyen gia tung linh vuc, phai dung dau bang goi y ---
        new NguoiDungMau("U05", "nguyenvanan", "Nguyễn Văn An", "Chuyên viên chính", "P01", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U06", "tranthibichngoc", "Trần Thị Bích Ngọc", "Chuyên viên chính", "P01", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U07", "levanminh", "Lê Văn Minh", "Phó Trưởng phòng", "P01", VaiTro.NguoiThucHien, 1, 10),
        new NguoiDungMau("U08", "phamthuhuong", "Phạm Thu Hương", "Chuyên viên", "P04", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U09", "hoangvantung", "Hoàng Văn Tùng", "Chuyên viên chính", "P04", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U10", "doanthimailinh", "Đoàn Thị Mai Linh", "Chuyên viên", "P03", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U11", "vuquanghuy", "Vũ Quang Huy", "Chuyên viên chính", "P03", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U12", "dangthikimoanh", "Đặng Thị Kim Oanh", "Chuyên viên", "P02", VaiTro.NguoiThucHien, 1, 8),

        // --- CHAN DUNG (b): gioi NHUNG dang qua tai (7 viec = 13,0 diem tai >= 8 x 1,5) ---
        new NguoiDungMau("U13", "buivanthanh", "Bùi Văn Thành", "Chuyên viên chính", "P01", VaiTro.NguoiThucHien, 1, 8),

        // --- CHAN DUNG (c): cham nhung ranh (tai = 0, dung han ~40%, bi tra lai 5 lan) ---
        new NguoiDungMau("U14", "ngothiphuongthao", "Ngô Thị Phương Thảo", "Chuyên viên", "P04", VaiTro.NguoiThucHien, 1, 8),

        // --- CHAN DUNG (d): nguoi moi tinh, KHONG co ban ghi nao (§9.5 cold start) ---
        new NguoiDungMau("U15", "dothiyennhi", "Đỗ Thị Yến Nhi", "Chuyên viên", "P01", VaiTro.NguoiThucHien, 1, 8),

        // --- CHAN DUNG (e): chi lam CNTT_HT, chua tung lam CNTT_ATTT (§9.4 S1 chiet khau 0,5) ---
        new NguoiDungMau("U16", "truongvanduc", "Trương Văn Đức", "Chuyên viên", "P01", VaiTro.NguoiThucHien, 1, 6),

        // --- CHAN DUNG (f): dang co 3 nhiem vu QUA HAN (§9.4 S5 = 0,00) ---
        new NguoiDungMau("U17", "phanvanquyet", "Phan Văn Quyết", "Chuyên viên", "P02", VaiTro.NguoiThucHien, 1, 8),

        // --- CHAN DUNG (g): tai khoan BI KHOA, ho so van dep (§9.3 dieu 1) ---
        new NguoiDungMau("U18", "hathiminhtam", "Hà Thị Minh Tâm", "Chuyên viên chính", "P03", VaiTro.NguoiThucHien, 0, 8),

        // --- CHAN DUNG (h): chuyen mon CNTT_ATTT cao nhung thuoc P02 (§9.3 dieu 3) ---
        new NguoiDungMau("U19", "caovanhieu", "Cao Văn Hiếu", "Chuyên viên chính", "P02", VaiTro.NguoiThucHien, 1, 8),

        // --- nhan su nen, ho so trung binh ---
        new NguoiDungMau("U20", "luuthihongnhung", "Lưu Thị Hồng Nhung", "Chuyên viên", "P03", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U21", "dinhvanlong", "Đinh Văn Long", "Chuyên viên", "P01", VaiTro.NguoiThucHien, 1, 8),
        new NguoiDungMau("U22", "maithianh", "Mai Thị Ánh", "Chuyên viên", "P04", VaiTro.NguoiThucHien, 1, 8)
    };

    /// <summary>Ban do chan dung (a)-(h) -&gt; ma nguoi dung noi bo.</summary>
    public static class ChanDung
    {
        public static readonly IReadOnlyList<string> AChuyenGia =
            new[] { "U05", "U06", "U07", "U08", "U09", "U10", "U11", "U12" };

        public const string BQuaTai = "U13";
        public const string CChamNhungRanh = "U14";
        public const string DNguoiMoi = "U15";
        public const string ELinhVucAnhEm = "U16";
        public const string FDangQuaHan = "U17";
        public const string GTaiKhoanKhoa = "U18";
        public const string HNgoaiPhamVi = "U19";
    }

    // ---------------------------------------------------------------------------------
    // Ho so sinh lich su cho 17 nguoi thuc hien (U15 nguoi moi: KHONG co).
    // Tong: 324 nhiem vu lich su (>= 320 theo yeu cau).
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<HoSoLichSuMau> HoSoLichSu = new[]
    {
        new HoSoLichSuMau("U05", new[] { ("CNTT_HT", 16), ("CNTT_BCVT", 5), ("CNTT_PM", 3) }, 0.88, 2, 2, 0, 5, 6),
        new HoSoLichSuMau("U06", new[] { ("CNTT_PM", 18), ("CNTT_HT", 4), ("CNTT_ATTT", 3) }, 0.86, 2, 2, 0, 4, 6),
        new HoSoLichSuMau("U07", new[] { ("CNTT_ATTT", 17), ("CNTT_HT", 5), ("CNTT_PM", 3) }, 0.90, 2, 1, 0, 5, 6),
        new HoSoLichSuMau("U08", new[] { ("HC_VT", 17), ("HC_TCCB", 5) }, 0.87, 2, 2, 0, 4, 6),
        new HoSoLichSuMau("U09", new[] { ("HC_TCCB", 15), ("HC_VT", 5) }, 0.85, 2, 2, 1, 4, 6),
        new HoSoLichSuMau("U10", new[] { ("KH_DT", 16), ("KH_TC", 4) }, 0.89, 2, 1, 0, 5, 6),
        new HoSoLichSuMau("U11", new[] { ("KH_TC", 17), ("KH_DT", 3) }, 0.88, 2, 2, 0, 4, 6),
        new HoSoLichSuMau("U12", new[] { ("CNTT_BCVT", 15), ("CNTT_HT", 4) }, 0.84, 3, 3, 1, 4, 5),
        new HoSoLichSuMau("U13", new[] { ("CNTT_PM", 14), ("CNTT_HT", 6), ("CNTT_ATTT", 3) }, 0.86, 3, 2, 0, 4, 6),
        new HoSoLichSuMau("U14", new[] { ("HC_VT", 7), ("HC_TCCB", 5), ("KH_TC", 3) }, 0.40, 5, 3, 1, 2, 4),
        new HoSoLichSuMau("U16", new[] { ("CNTT_HT", 7) }, 0.83, 1, 1, 0, 4, 5),
        new HoSoLichSuMau("U17", new[] { ("CNTT_BCVT", 8), ("CNTT_HT", 4) }, 0.64, 4, 3, 1, 3, 5),
        new HoSoLichSuMau("U18", new[] { ("HC_TCCB", 12), ("HC_VT", 4) }, 0.87, 2, 2, 0, 4, 6),
        new HoSoLichSuMau("U19", new[] { ("CNTT_ATTT", 13), ("CNTT_BCVT", 5) }, 0.88, 2, 2, 0, 5, 6),
        new HoSoLichSuMau("U20", new[] { ("KH_DT", 9), ("KH_TC", 7), ("HC_VT", 3) }, 0.72, 4, 3, 1, 3, 5),
        new HoSoLichSuMau("U21", new[] { ("CNTT_PM", 8), ("CNTT_HT", 7), ("CNTT_ATTT", 4) }, 0.70, 4, 3, 1, 3, 5),
        new HoSoLichSuMau("U22", new[] { ("HC_VT", 9), ("HC_TCCB", 7), ("KH_TC", 4) }, 0.75, 4, 3, 1, 3, 5)
    };

    // ---------------------------------------------------------------------------------
    // Danh muc loai van ban / co quan / nguon nhiem vu
    // ---------------------------------------------------------------------------------
    public static readonly IReadOnlyList<string> LoaiVb = new[]
    {
        "QUYETDINH", "KEHOACH", "CONGVAN", "THONGBAO", "CHUONGTRINH"
    };

    /// <summary>Nhan hien thi cua tung loai van ban (seed vao DM_TUDIEN type LOAIVB).</summary>
    public static readonly IReadOnlyList<(string Ma, string Nhan)> NhanLoaiVb = new[]
    {
        ("QUYETDINH", "Quyết định"),
        ("KEHOACH", "Kế hoạch"),
        ("CONGVAN", "Công văn"),
        ("THONGBAO", "Thông báo"),
        ("CHUONGTRINH", "Chương trình")
    };

    public static readonly IReadOnlyDictionary<string, string> HauToSoKyHieu =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["QUYETDINH"] = "QĐ-STTTT",
            ["KEHOACH"] = "KH-STTTT",
            ["CONGVAN"] = "STTTT-VP",
            ["THONGBAO"] = "TB-STTTT",
            ["CHUONGTRINH"] = "CTr-STTTT"
        };

    public static readonly IReadOnlyList<string> CoQuanBanHanh = new[]
    {
        "Sở Thông tin và Truyền thông",
        "Sở Thông tin và Truyền thông",
        "Ủy ban nhân dân tỉnh",
        "Bộ Thông tin và Truyền thông"
    };

    public static readonly IReadOnlyList<string> NguonNhiemVu = new[]
    {
        "Giám đốc Sở", "Ủy ban nhân dân tỉnh", "Bộ Thông tin và Truyền thông", "Lãnh đạo Sở"
    };
}
