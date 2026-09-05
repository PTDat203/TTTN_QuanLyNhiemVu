using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Abstractions;

/// <summary>
/// Anh chup du lieu can cho dong co goi y (§9) va cho do hieu qua (§9.8).
///
/// LY DO TON TAI: <see cref="Services.RecommendationService"/> va
/// <see cref="Services.MetricsService"/> deu la thuat toan THUAN TUY tren bo nho.
/// Tach phan doc du lieu ra interface nay de:
///  - QLNV.Core khong phu thuoc EF Core / CSDL nao (rang buoc cua project);
///  - kiem thu duoc toan bo cong thuc §9.4 / §9.8 ma KHONG can DB.
///
/// Tang Infrastructure cai dat <see cref="IAiDataSource"/> bang mot truy van EF Core
/// (voi <c>AsNoTracking()</c>): quy mo du lieu cua de tai (&lt; 5.000 nhiem vu, §4.8)
/// cho phep nap thang vao bo nho.
/// </summary>
public sealed class AiSnapshot
{
    /// <summary>Ngay dung lam moc tinh han / tinh tai. Mac dinh la ngay he thong.</summary>
    public DateOnly HomNay { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Toan bo nguoi dung (bo loc cung §9.3 tu loc ra ung vien hop le).</summary>
    public IReadOnlyList<SysUser> NguoiDung { get; set; } = Array.Empty<SysUser>();

    /// <summary>Toan bo nhiem vu — §9.4 dem tren tap nay.</summary>
    public IReadOnlyList<DmNhiemVuChiTiet> NhiemVu { get; set; } = Array.Empty<DmNhiemVuChiTiet>();

    /// <summary>Ban ghi phan cong (ke ca ban da thu hoi <c>trangthai = 0</c> — bo loc tu bo).</summary>
    public IReadOnlyList<NhiemVuPhanCong> PhanCong { get; set; } = Array.Empty<NhiemVuPhanCong>();

    /// <summary>Lich su xu ly — §9.3 dieu 5 (TUCHOI) va §9.4 S3(b) (truc B = 12).</summary>
    public IReadOnlyList<XuLyNhiemVu> XuLy { get; set; } = Array.Empty<XuLyNhiemVu>();

    /// <summary>Danh muc linh vuc — can <c>NhomCha</c> cho nhanh chiet khau 50% cua §9.4 S1.</summary>
    public IReadOnlyList<DmLinhVuc> LinhVuc { get; set; } = Array.Empty<DmLinhVuc>();

    /// <summary>Danh muc don vi — chi de hien thi <c>unitname</c> (§9.6).</summary>
    public IReadOnlyList<SysUnit> DonVi { get; set; } = Array.Empty<SysUnit>();

    /// <summary>Lich su gia han — chi <see cref="Services.MetricsService"/> dung (dung lai moc thoi gian §9.8).</summary>
    public IReadOnlyList<GiaHanNhiemVu> GiaHan { get; set; } = Array.Empty<GiaHanNhiemVu>();

    /// <summary>Nhat ky goi y (§4.8) — chi <see cref="Services.MetricsService"/> dung.</summary>
    public IReadOnlyList<AiGoiYLog> NhatKyAi { get; set; } = Array.Empty<AiGoiYLog>();
}

/// <summary>
/// Cong doc/ghi du lieu cua phan he AI. Tang Infrastructure cai dat.
/// Moi phuong thuc deu bat dong bo vi cai dat that se cham CSDL.
/// </summary>
public interface IAiDataSource
{
    /// <summary>
    /// Nap anh chup phuc vu cham diem (§9.4) va do hieu qua (§9.8).
    /// </summary>
    /// <param name="dayDu">
    /// <c>false</c> = chi can du lieu cho goi y (nguoi dung, nhiem vu, phan cong, xu ly, danh muc);
    /// <c>true</c> = nap them <c>GiaHan</c> va <c>NhatKyAi</c> cho §9.8.
    /// </param>
    Task<AiSnapshot> LaySnapshotAsync(bool dayDu, CancellationToken ct);

    /// <summary>Doc mot ban ghi <c>AI_GOIY_LOG</c> theo khoa (§5.8 H2).</summary>
    Task<AiGoiYLog?> LayNhatKyAsync(Guid goiYId, CancellationToken ct);

    /// <summary>Ghi moi mot ban ghi <c>AI_GOIY_LOG</c> (§4.8) ngay sau khi tra goi y.</summary>
    Task ThemNhatKyAsync(AiGoiYLog banGhi, CancellationToken ct);

    /// <summary>Cap nhat ban ghi <c>AI_GOIY_LOG</c> sau khi nguoi giao chot nguoi thuc hien (§5.8 H2).</summary>
    Task CapNhatNhatKyAsync(AiGoiYLog banGhi, CancellationToken ct);

    /// <summary>
    /// Doc bo trong so da luu (§5.8 H4). Tra <c>null</c> khi chua tung luu —
    /// khi do dung bo mac dinh trong <see cref="Services.CauHinhAi"/>.
    /// </summary>
    Task<CauHinhAiDto?> DocCauHinhAsync(CancellationToken ct);

    /// <summary>Luu bo trong so (§5.8 H4).</summary>
    Task LuuCauHinhAsync(CauHinhAiDto cauHinh, CancellationToken ct);
}
