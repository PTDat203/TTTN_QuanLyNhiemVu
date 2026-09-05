using Microsoft.EntityFrameworkCore;
using QLNV.Core.Abstractions;
using QLNV.Infrastructure.Data.Seed;

namespace QLNV.Infrastructure.Data;

/// <summary>
/// Khoi tao CSDL: dung luoc do (schema) roi nap bo du lieu mau neu CSDL con rong.
///
/// ⚠ CHUA CO MIGRATION: may phat trien khong co .NET SDK nen KHONG chay duoc
/// <c>dotnet ef migrations add InitialCreate</c>. Vi vay lop nay dung
/// <c>Database.EnsureCreatedAsync()</c> — du de chay
/// demo tren SQLite, NHUNG khong nang cap duoc luoc do.
/// KHI CO SDK, viec dau tien phai lam la:
///     dotnet ef migrations add InitialCreate -p src/QLNV.Infrastructure -s src/QLNV.Api
/// roi doi <see cref="DungLuocDoAsync"/> sang goi <c>MigrateAsync()</c>.
/// </summary>
public sealed class DbInitializer
{
    private readonly QlnvDbContext _db;
    private readonly IPasswordHasher _bamMatKhau;

    public DbInitializer(QlnvDbContext db, IPasswordHasher bamMatKhau)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _bamMatKhau = bamMatKhau ?? throw new ArgumentNullException(nameof(bamMatKhau));
    }

    /// <summary>
    /// Dung luoc do neu chua co. Tra ve <c>true</c> khi vua tao moi CSDL.
    /// </summary>
    /// <param name="dungMigration">
    /// <c>true</c> — ap dung migration (chi dung khi da sinh duoc thu muc Migrations);
    /// <c>false</c> — <c>EnsureCreatedAsync</c> (mac dinh o moi truong phat trien).
    /// </param>
    public async Task<bool> DungLuocDoAsync(bool dungMigration, CancellationToken ct = default)
    {
        if (dungMigration)
        {
            await _db.Database.MigrateAsync(ct).ConfigureAwait(false);
            return true;
        }

        return await _db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Nap bo du lieu mau. KHONG lam gi neu bang <c>SYS_USER</c> da co ban ghi —
    /// nho vay goi lai nhieu lan van an toan.
    /// </summary>
    /// <param name="homNay">Ngay lam moc. Null =&gt; ngay moc mac dinh cua bo du lieu mau.</param>
    /// <returns>So ban ghi da chen; 0 neu CSDL da co du lieu.</returns>
    public async Task<int> NapDuLieuMauAsync(DateOnly? homNay = null, CancellationToken ct = default)
    {
        if (await _db.NguoiDung.AnyAsync(ct).ConfigureAwait(false)) return 0;

        var mau = BoSinhDuLieuMau.Tao(homNay, _bamMatKhau);

        // Thu tu chen bam do thi phu thuoc (§4.9). EF Core tu sap xep lai theo khoa ngoai,
        // nhung giu dung thu tu o day de doc ma de hieu va de go loi khi doi provider.
        _db.DonVi.AddRange(mau.DonVi);
        _db.LinhVuc.AddRange(mau.LinhVuc);
        _db.TuDien.AddRange(mau.TuDien);
        _db.NguoiDung.AddRange(mau.NguoiDung);
        _db.VanBan.AddRange(mau.VanBan);
        _db.NhiemVu.AddRange(mau.NhiemVu);
        _db.PhanCong.AddRange(mau.PhanCong);
        _db.XuLy.AddRange(mau.XuLy);
        _db.GiaHan.AddRange(mau.GiaHan);
        _db.AiGoiYLog.AddRange(mau.AiLog);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return mau.DonVi.Count + mau.LinhVuc.Count + mau.TuDien.Count + mau.NguoiDung.Count
               + mau.VanBan.Count + mau.NhiemVu.Count + mau.PhanCong.Count + mau.XuLy.Count
               + mau.GiaHan.Count + mau.AiLog.Count;
    }

    /// <summary>
    /// Tien ich goi mot phat luc khoi dong ung dung: dung luoc do + nap du lieu mau.
    /// </summary>
    public async Task<string> KhoiTaoAsync(
        bool dungMigration = false,
        bool napDuLieuMau = true,
        DateOnly? homNay = null,
        CancellationToken ct = default)
    {
        bool vuaTao = await DungLuocDoAsync(dungMigration, ct).ConfigureAwait(false);
        if (!napDuLieuMau)
        {
            return vuaTao ? "Đã tạo lược đồ cơ sở dữ liệu (chưa nạp dữ liệu mẫu)."
                          : "Cơ sở dữ liệu đã sẵn sàng (chưa nạp dữ liệu mẫu).";
        }

        int soBanGhi = await NapDuLieuMauAsync(homNay, ct).ConfigureAwait(false);
        return soBanGhi == 0
            ? "Cơ sở dữ liệu đã có dữ liệu, bỏ qua bước nạp dữ liệu mẫu."
            : $"Đã nạp {soBanGhi} bản ghi dữ liệu mẫu vào cơ sở dữ liệu.";
    }

    /// <summary>
    /// XOA SACH va nap lai bo du lieu mau. CHI dung o moi truong phat trien/demo.
    /// </summary>
    public async Task<string> NapLaiAsync(DateOnly? homNay = null, CancellationToken ct = default)
    {
        await _db.Database.EnsureDeletedAsync(ct).ConfigureAwait(false);
        await _db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
        int soBanGhi = await NapDuLieuMauAsync(homNay, ct).ConfigureAwait(false);
        return $"Đã xoá và nạp lại {soBanGhi} bản ghi dữ liệu mẫu.";
    }
}
