using QLNV.Core.Abstractions;
using QLNV.Core.Entities;

using CDoKhan = QLNV.Core.Constants.DoKhan;
using CTrangThaiGiaHan = QLNV.Core.Constants.TrangThaiGiaHan;
using CTrangThaiNv = QLNV.Core.Constants.TrangThaiNv;
using CTrangThaiPhanCong = QLNV.Core.Constants.TrangThaiPhanCong;
using CVaiTro = QLNV.Core.Constants.VaiTro;
using CVaiTroPhanCong = QLNV.Core.Constants.VaiTroPhanCong;

namespace QLNV.Tests.HoTro;

/// <summary>
/// XUONG DUNG DU LIEU KIEM THU — dung thuc the cua QLNV.Core, KHONG cham CSDL.
///
/// Ca <c>INhiemVuStateMachine</c> lan <c>IQuyenService</c> deu thuan tuy (§ hop dong o
/// <see cref="NguCanh"/>), nen moi test may trang thai / phan quyen chi can doi tuong dung san.
/// Moc thoi gian LUON lay tu <see cref="HomNay"/> — khong dung <c>DateTime.Now</c> de test
/// lap lai duoc.
/// </summary>
internal static class Xuong
{
    /// <summary>Moc "hom nay" co dinh cua toan bo bo test.</summary>
    public static readonly DateOnly HomNay = new(2026, 9, 5);

    /// <summary>Han CON HIEU LUC so voi <see cref="HomNay"/> (con 15 ngay).</summary>
    public static readonly DateOnly HanConHan = new(2026, 9, 20);

    /// <summary>Han DA QUA so voi <see cref="HomNay"/> (tre 16 ngay).</summary>
    public static readonly DateOnly HanQuaHan = new(2026, 8, 20);

    /// <summary>Han dung BANG hom nay — §2.5: so ngay con lai = 0 van tinh la CON HAN.</summary>
    public static readonly DateOnly HanDungHomNay = new(2026, 9, 5);

    private static readonly DateTime MocTao = new(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);

    // ---------------------------------------------------------------------
    // Nguoi dung (§4.7)
    // ---------------------------------------------------------------------

    /// <summary>Tao mot <see cref="SysUser"/> voi vai tro chi dinh (§6.1 — 1 truc vai tro).</summary>
    public static SysUser NguoiDung(
        string vaiTro,
        Guid? id = null,
        int trangThai = 1,
        int maxConcurrentTasks = 8,
        string unitCode = "P01",
        string? hoTen = null)
    {
        var ma = id ?? Guid.NewGuid();
        return new SysUser
        {
            Id = ma,
            UserName = "u" + ma.ToString("N")[..8],
            PasswordHash = "khong-dung-trong-test",
            FullName = hoTen ?? ("Người dùng " + ma.ToString("N")[..4]),
            UnitCode = unitCode,
            ChucVu = "Chuyên viên",
            VaiTro = vaiTro,
            TrangThai = trangThai,
            MaxConcurrentTasks = maxConcurrentTasks,
            CreateDate = MocTao
        };
    }

    /// <summary>§6.2 cot NGUOI_GIAO.</summary>
    public static SysUser NguoiGiao(Guid? id = null, int trangThai = 1)
        => NguoiDung(CVaiTro.NguoiGiao, id, trangThai, hoTen: "Người giao việc");

    /// <summary>§6.2 cot NGUOI_THUC_HIEN.</summary>
    public static SysUser NguoiThucHien(Guid? id = null, int trangThai = 1, int maxConcurrentTasks = 8)
        => NguoiDung(CVaiTro.NguoiThucHien, id, trangThai, maxConcurrentTasks, hoTen: "Người thực hiện");

    /// <summary>§6.2 cot QUAN_TRI.</summary>
    public static SysUser QuanTri(Guid? id = null, int trangThai = 1)
        => NguoiDung(CVaiTro.QuanTri, id, trangThai, hoTen: "Quản trị hệ thống");

    // ---------------------------------------------------------------------
    // Nhiem vu (§4.2)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Tao mot nhiem vu o cap trang thai <paramref name="trangThai"/> / <paramref name="trangThaiDvXuly"/>
    /// (§2.1 truc A, §2.2 truc B).
    /// </summary>
    public static DmNhiemVuChiTiet NhiemVu(
        Guid nguoiGiaoId,
        int trangThai = CTrangThaiNv.ChuaTrienKhai,
        int? trangThaiDvXuly = null,
        DateOnly? hanXuLyTh = null,
        int? trangThaiXuLyGiaHan = null,
        int soLanGiaHan = 0,
        Guid? id = null,
        string? linhVuc = "CNTT_PM",
        string doKhan = CDoKhan.ThuongXuyen)
    {
        return new DmNhiemVuChiTiet
        {
            Id = id ?? Guid.NewGuid(),
            IdVb = Guid.NewGuid(),
            NoiDung = "Rà soát và báo cáo tình hình triển khai nhiệm vụ được giao.",
            LinhVuc = linhVuc,
            DoKhan = doKhan,
            HanXuLyTh = hanXuLyTh ?? HanConHan,
            NgayGiao = MocTao,
            TrangThai = trangThai,
            TrangThaiDvXuly = trangThaiDvXuly,
            TrangThaiXuLyGiaHan = trangThaiXuLyGiaHan,
            SoLanGiaHan = soLanGiaHan,
            UserIdGiaoViec = nguoiGiaoId,
            UserIdCreate = nguoiGiaoId,
            UnitCode = "P01",
            CreateDate = MocTao,
            UpdateDate = MocTao
        };
    }

    // ---------------------------------------------------------------------
    // Phan cong (§4.3)
    // ---------------------------------------------------------------------

    /// <summary>Ban ghi phan cong vai CHUTRI con hieu luc.</summary>
    public static NhiemVuPhanCong ChuTri(Guid idNhiemVu, Guid userId, int trangThai = CTrangThaiPhanCong.ConHieuLuc)
        => PhanCong(idNhiemVu, userId, CVaiTroPhanCong.ChuTri, trangThai);

    /// <summary>Ban ghi phan cong vai PHOIHOP con hieu luc (§6.3).</summary>
    public static NhiemVuPhanCong PhoiHop(Guid idNhiemVu, Guid userId, int trangThai = CTrangThaiPhanCong.ConHieuLuc)
        => PhanCong(idNhiemVu, userId, CVaiTroPhanCong.PhoiHop, trangThai);

    /// <summary>Ban ghi phan cong tong quat.</summary>
    public static NhiemVuPhanCong PhanCong(Guid idNhiemVu, Guid userId, string vaiTro, int trangThai)
        => new()
        {
            Id = Guid.NewGuid(),
            IdNvChiTiet = idNhiemVu,
            UserId = userId,
            UnitCode = "P01",
            VaiTro = vaiTro,
            UserIdCreate = userId,
            TrangThai = trangThai,
            CreateDate = MocTao
        };

    // ---------------------------------------------------------------------
    // Gia han (§4.5)
    // ---------------------------------------------------------------------

    /// <summary>De xuat gia han DANG CHO DUYET (truc C = 10) — bat buoc cho §2.4 T12/T13.</summary>
    public static GiaHanNhiemVu DeXuatGiaHan(
        Guid idNhiemVu,
        Guid nguoiDeXuat,
        DateOnly hanDeXuat,
        DateOnly? hanCu = null,
        int? trangThaiCu = CTrangThaiNv.DangTrienKhai)
        => new()
        {
            Id = Guid.NewGuid(),
            IdCtnv = idNhiemVu,
            NoiDung = "Khối lượng công việc phát sinh, đề nghị cho gia hạn.",
            HanXuLyDeXuat = hanDeXuat,
            HanXuLyThCu = hanCu ?? HanConHan,
            TrangThai = CTrangThaiGiaHan.ChoDuyet,
            UserIdDeXuat = nguoiDeXuat,
            CreateDate = MocTao,
            TrangThaiCu = trangThaiCu
        };

    // ---------------------------------------------------------------------
    // Ngu canh (§ hop dong NguCanh)
    // ---------------------------------------------------------------------

    /// <summary>Ngu canh toi thieu: hom nay co dinh + nguoi thao tac + danh sach phan cong.</summary>
    public static NguCanh Ctx(SysUser nguoiThaoTac, params NhiemVuPhanCong[] phanCong)
        => NguCanh.Tao(HomNay, nguoiThaoTac, phanCong);

    /// <summary>Ngu canh voi moc "hom nay" tu chon.</summary>
    public static NguCanh Ctx(DateOnly homNay, SysUser nguoiThaoTac, params NhiemVuPhanCong[] phanCong)
        => NguCanh.Tao(homNay, nguoiThaoTac, phanCong);
}
