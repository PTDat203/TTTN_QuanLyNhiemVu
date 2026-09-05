using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Core.Abstractions;

/// <summary>
/// NGU CANH cua mot hanh dong tren may trang thai.
///
/// MUC DICH THIET KE (quan trong nhat cua project): gom TAT CA du lieu ma
/// <see cref="INhiemVuStateMachine"/> va <see cref="IQuyenService"/> can, de hai thanh phan
/// nay THUAN TUY — khong truy CSDL, khong doc dong ho he thong, khong goi dich vu ngoai.
/// Nho vay kiem thu xUnit dung duoc voi doi tuong dung san, khong can DbContext.
///
/// Tang tren (controller / service co DbContext) chiu trach nhiem NAP day du ngu canh
/// truoc khi goi, va GHI lai cac ban ghi phat sinh trong <see cref="KetXuat"/> sau khi goi.
/// </summary>
public sealed record NguCanh
{
    /// <param name="homNay">Ngay dung lam moc so han (§2.5, §5.4 D5). Truyen vao de test lap lai duoc.</param>
    /// <param name="nguoiThaoTac">
    /// Nguoi dang thuc hien hanh dong (co the la nguoi giao HOAC nguoi thuc hien, tuy hanh dong).
    /// Can <c>VaiTro</c> cho lop 1 va <c>TrangThai</c> cho §9.3 dieu 1 / §6.4.
    /// </param>
    /// <param name="phanCong">TOAN BO ban ghi phan cong cua CHINH nhiem vu dang thao tac, ke ca ban ghi da thu hoi.</param>
    public NguCanh(DateOnly homNay, SysUser nguoiThaoTac, IReadOnlyList<NhiemVuPhanCong> phanCong)
    {
        HomNay = homNay;
        NguoiThaoTac = nguoiThaoTac;
        PhanCong = phanCong;
    }

    /// <summary>Ngay dung lam moc so han. KHONG duoc lay tu <c>DateTime.Now</c> ben trong may trang thai.</summary>
    public DateOnly HomNay { get; init; }

    /// <summary>Nguoi dang thao tac.</summary>
    public SysUser NguoiThaoTac { get; init; }

    /// <summary>
    /// Cac ban ghi phan cong cua nhiem vu dang thao tac (§4.3).
    /// Bao gom ca ban ghi <c>trangthai = 0</c> (da thu hoi) — cac ham loc se tu bo qua.
    /// </summary>
    public IReadOnlyList<NhiemVuPhanCong> PhanCong { get; init; }

    /// <summary>
    /// De xuat gia han DANG CHO DUYET (<c>trangThai = 10</c>) cua nhiem vu, neu co.
    /// BAT BUOC cho §2.4 T12/T13; co the null o moi hanh dong khac.
    /// </summary>
    public GiaHanNhiemVu? GiaHanChoDuyet { get; init; }

    /// <summary>
    /// Toan bo lich su xu ly cua nhiem vu (§4.4). Tuy chon — chi can khi hanh dong phai
    /// tra cuu qua khu. Mac dinh rong.
    /// </summary>
    public IReadOnlyList<XuLyNhiemVu> LichSuXuLy { get; init; } = Array.Empty<XuLyNhiemVu>();

    /// <summary>
    /// Bo cau hinh nghiep vu (nguong sap het han, so lan gia han toi da, do dai noi dung).
    /// Mac dinh <see cref="CauHinhNghiepVu.MacDinh"/>.
    /// </summary>
    public CauHinhNghiepVu CauHinh { get; init; } = CauHinhNghiepVu.MacDinh;

    /// <summary>
    /// KENH XUAT cua may trang thai: cac ban ghi phu sinh ra trong hanh dong
    /// (lich su xu ly, de xuat gia han moi, de xuat gia han bi sua).
    ///
    /// Ly do ton tai: dac ta yeu cau moi ham hanh dong tra <c>Result&lt;DmNhiemVuChiTiet&gt;</c>,
    /// nen khong the tra kem cac ban ghi phu. Doi tuong nay dong vai tro "hop thu di":
    /// may trang thai CHI them vao day, tang tren doc ra roi ghi xuong CSDL.
    /// May trang thai van thuan tuy theo nghia khong cham I/O.
    /// </summary>
    public KetXuatHanhDong KetXuat { get; init; } = new();

    /// <summary>Cac ban ghi phan cong CON HIEU LUC (<c>trangthai = 1</c>).</summary>
    public IEnumerable<NhiemVuPhanCong> PhanCongConHieuLuc =>
        PhanCong.Where(p => p.TrangThai == TrangThaiPhanCong.ConHieuLuc);

    /// <summary>True neu <paramref name="userId"/> dang giu vai CHUTRI con hieu luc.</summary>
    public bool LaChuTri(Guid userId) =>
        PhanCong.Any(p => p.UserId == userId
                          && p.TrangThai == TrangThaiPhanCong.ConHieuLuc
                          && p.VaiTro == VaiTroPhanCong.ChuTri);

    /// <summary>True neu <paramref name="userId"/> dang giu vai PHOIHOP con hieu luc.</summary>
    public bool LaPhoiHop(Guid userId) =>
        PhanCong.Any(p => p.UserId == userId
                          && p.TrangThai == TrangThaiPhanCong.ConHieuLuc
                          && p.VaiTro == VaiTroPhanCong.PhoiHop);

    /// <summary>
    /// True neu <paramref name="userId"/> DA TUNG tu choi chinh nhiem vu nay (§9.3 dieu 5).
    /// Can <see cref="LichSuXuLy"/> duoc nap.
    /// </summary>
    public bool DaTungTuChoi(Guid userId) =>
        LichSuXuLy.Any(x => x.UserIdXuLy == userId && x.Loai == LoaiXuLy.TuChoi);

    /// <summary>Tao ngu canh toi thieu — tien cho kiem thu.</summary>
    public static NguCanh Tao(DateOnly homNay, SysUser nguoiThaoTac, params NhiemVuPhanCong[] phanCong) =>
        new(homNay, nguoiThaoTac, phanCong);
}

/// <summary>
/// Bo cau hinh nghiep vu co the tinh chinh. Tach khoi <see cref="Constants.GioiHan"/>
/// de kiem thu doi khang doi duoc nguong ma khong dung den hang so bien dich.
/// </summary>
public sealed record CauHinhNghiepVu
{
    /// <summary>Bo gia tri mac dinh dung dac ta.</summary>
    public static readonly CauHinhNghiepVu MacDinh = new();

    /// <summary>§2.3 — toi da 2 lan gia han.</summary>
    public int SoLanGiaHanToiDa { get; init; } = GioiHan.SoLanGiaHanToiDa;

    /// <summary>§4.2 / §4.4 — noi dung toi da 2000 ky tu.</summary>
    public int DoDaiNoiDungToiDa { get; init; } = GioiHan.DoDaiNoiDung;

    /// <summary>§3.1 / §3.3 — nguong "sap het han" = 3 ngay.</summary>
    public int NguongSapHetHan { get; init; } = GioiHan.NguongSapHetHan;

    /// <summary>
    /// §6.2 — quan tri co bo qua dieu kien SO HUU du lieu hay khong.
    /// Quyet dinh da chot (bam ban flow.js): quan tri bo qua dieu kien so huu NHUNG
    /// VAN phai thoa dieu kien TRANG THAI — hanh dong sai trang thai la vo nghia.
    /// </summary>
    public bool QuanTriToanQuyen { get; init; } = true;
}

/// <summary>
/// Cac ban ghi phat sinh khi may trang thai chay xong mot hanh dong.
/// Tang tren doc ra roi ghi xuong CSDL trong cung mot giao dich voi nhiem vu.
/// </summary>
public sealed class KetXuatHanhDong
{
    /// <summary>Ban ghi lich su xu ly moi (§4.4) — thuong dung 1 ban ghi cho moi hanh dong.</summary>
    public List<XuLyNhiemVu> LichSuXuLyMoi { get; } = new();

    /// <summary>De xuat gia han MOI can chen (§2.4 T11).</summary>
    public List<GiaHanNhiemVu> GiaHanMoi { get; } = new();

    /// <summary>
    /// De xuat gia han DA CO nhung bi sua trang thai (§2.4 T12/T13, va truong hop T14
    /// dong cac de xuat con treo).
    /// </summary>
    public List<GiaHanNhiemVu> GiaHanCapNhat { get; } = new();

    /// <summary>Xoa sach ket xuat — goi truoc khi tai su dung ngu canh trong kiem thu.</summary>
    public void XoaHet()
    {
        LichSuXuLyMoi.Clear();
        GiaHanMoi.Clear();
        GiaHanCapNhat.Clear();
    }
}

/// <summary>
/// §6.1 — vai tro cua mot nguoi TREN MOT NHIEM VU cu the, tinh tu trang thai + bang phan cong.
/// Ham thuan tuy, khong cham CSDL.
/// </summary>
public sealed record VaiTroNhiemVu(
    bool LaNguoiGiao,
    bool LaNguoiTao,
    bool LaChuTri,
    bool LaPhoiHop,
    bool LaQuanTri)
{
    /// <summary>Khong mang vai tro nao (tai khoan khoa, khong ton tai, khong lien quan).</summary>
    public static readonly VaiTroNhiemVu KhongCo = new(false, false, false, false, false);

    /// <summary>True neu co bat ky lien he nao voi nhiem vu (§6.2 dong 18, 19).</summary>
    public bool CoLienQuan => LaNguoiGiao || LaNguoiTao || LaChuTri || LaPhoiHop || LaQuanTri;

    /// <summary>
    /// Tinh vai tro cua <paramref name="userId"/> tren <paramref name="nv"/>.
    /// Tai khoan bi khoa (<c>SYS_USER.trangthai = 0</c>) KHONG mang vai tro nao — moi quyen o
    /// §6.2 deu tat (§6.4: backend phai tu kiem, khong tin co do FE gui).
    /// </summary>
    public static VaiTroNhiemVu Tinh(DmNhiemVuChiTiet? nv, Guid userId, NguCanh ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var u = ctx.NguoiThaoTac;
        if (userId == Guid.Empty || u.Id != userId || !u.DangHoatDong) return KhongCo;

        var laQuanTri = u.VaiTro == Constants.VaiTro.QuanTri;
        if (nv is null) return new VaiTroNhiemVu(false, false, false, false, laQuanTri);

        return new VaiTroNhiemVu(
            LaNguoiGiao: nv.UserIdGiaoViec != Guid.Empty && nv.UserIdGiaoViec == userId,
            LaNguoiTao: nv.UserIdCreate != Guid.Empty && nv.UserIdCreate == userId,
            LaChuTri: ctx.LaChuTri(userId),
            LaPhoiHop: ctx.LaPhoiHop(userId),
            LaQuanTri: laQuanTri);
    }
}
