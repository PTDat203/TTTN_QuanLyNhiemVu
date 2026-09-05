using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Ai;

/// <summary>
/// ⚠ LUU Y CHO TAC TU SAU: dac ta cong viec ghi "cai <c>IAiDataSource</c> CUA CORE",
/// nhung BAN KE KIEU cua <c>QLNV.Core</c> (hop dong da chot) KHONG co interface nay.
/// De khong tu y sua hop dong Core, interface duoc khai o day
/// (<c>QLNV.Infrastructure.Ai</c>). Neu sau nay muon dua no ve Core, chi can chuyen tep
/// va doi namespace — <see cref="EfAiDataSource"/> khong phai sua gi khac.
///
/// Vai tro: nap MOT LAN toan bo du lieu tho ma bo cham diem §9.4 can, roi tra ve dang
/// "anh chup" trong bo nho. Ban ai-engine.js da kiem chung lam viec tren mot doi tuong
/// <c>db</c> nhu vay (<c>xayDungChiMuc</c>), nen cach nay giu nguyen duoc cau truc thuat toan
/// khi port sang C#.
///
/// Voi quy mo cua de tai (&lt; 5.000 nhiem vu — nguong ma §4.8 cho phep bo bang tong hop
/// <c>USER_HIEUSUAT</c>) mot lan nap day du chi ton vai chuc mili giay, doi lai duoc
/// mot bo cham diem THUAN TUY, de kiem thu va giai thich duoc khi bao ve.
/// </summary>
public interface IAiDataSource
{
    /// <summary>Nap anh chup toan bo du lieu can cho §9.3 (loc cung) va §9.4 (cham diem).</summary>
    Task<AnhChupAi> TaiAnhChupAsync(CancellationToken ct);

    /// <summary>
    /// Nap anh chup GON hon, chi gom nguoi dung thuoc cac don vi trong pham vi.
    /// Danh sach rong / null =&gt; lay toan bo (khong loc theo don vi).
    /// </summary>
    Task<AnhChupAi> TaiAnhChupAsync(IReadOnlyCollection<string>? phamViUnitCode, CancellationToken ct);
}

/// <summary>
/// Anh chup du lieu tho phuc vu bo cham diem AI (§9.3, §9.4).
/// Moi danh sach deu la ban ghi KHONG theo doi (AsNoTracking) — chi doc.
/// </summary>
public sealed class AnhChupAi
{
    /// <summary>Ngay lam moc khi tinh (dung cho "qua han", "sap het han").</summary>
    public DateOnly HomNay { get; init; }

    /// <summary>Toan bo nguoi dung trong pham vi (ke ca tai khoan bi khoa — §9.3 dieu 1 tu loc).</summary>
    public IReadOnlyList<SysUser> NguoiDung { get; init; } = Array.Empty<SysUser>();

    /// <summary>Cay don vi — dung cho loc pham vi §9.3 dieu 3.</summary>
    public IReadOnlyList<SysUnit> DonVi { get; init; } = Array.Empty<SysUnit>();

    /// <summary>Danh muc linh vuc co phan cap — dung cho §9.4 S1 (chiet khau nhom cha).</summary>
    public IReadOnlyList<DmLinhVuc> LinhVuc { get; init; } = Array.Empty<DmLinhVuc>();

    /// <summary>Nhiem vu — dung cho S1, S2, S3, S4, S5.</summary>
    public IReadOnlyList<DmNhiemVuChiTiet> NhiemVu { get; init; } = Array.Empty<DmNhiemVuChiTiet>();

    /// <summary>Phan cong (ke ca ban ghi da thu hoi) — bo cham diem tu loc <c>trangthai = 1</c>.</summary>
    public IReadOnlyList<NhiemVuPhanCong> PhanCong { get; init; } = Array.Empty<NhiemVuPhanCong>();

    /// <summary>
    /// Lich su xu ly — chi lay hai loai co anh huong toi cham diem:
    /// <c>TUCHOI</c> (§9.3 dieu 5) va ban ghi co truc B = 12 (§9.4 S3b).
    /// </summary>
    public IReadOnlyList<XuLyNhiemVu> XuLy { get; init; } = Array.Empty<XuLyNhiemVu>();
}
