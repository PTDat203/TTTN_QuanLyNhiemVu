using Microsoft.EntityFrameworkCore;

using QLNV.Core.Abstractions;
using QLNV.Core.Entities;

using QLNV.Infrastructure.Data;
using QLNV.Infrastructure.Services;

namespace QLNV.Tests.HoTro;

/// <summary>
/// CAU NOI DUY NHAT giua bo test va QLNV.Infrastructure.
///
/// MOI tep test khac chi duoc dung KIEU CUA QLNV.Core (thuc the, DTO, interface).
/// Neu tang ha tang doi ten lop cai dat thi CHI phai sua tep nay.
///
/// HOP DONG BAT BUOC cua QLNV.Infrastructure (da thong nhat khi chia viec):
///   - <c>QLNV.Infrastructure.Data.QlnvDbContext(DbContextOptions&lt;QlnvDbContext&gt;)</c>
///   - <c>QLNV.Infrastructure.Data.DbInitializer.SeedAsync(QlnvDbContext, CancellationToken)</c>
///   - <c>QLNV.Infrastructure.Services.NhiemVuStateMachine()</c>      : INhiemVuStateMachine
///   - <c>QLNV.Infrastructure.Services.QuyenService()</c>              : IQuyenService
///   - <c>QLNV.Infrastructure.Services.RecommendationService(QlnvDbContext)</c> : IRecommendationService
///   - <c>QLNV.Infrastructure.Services.MetricsService(QlnvDbContext)</c>        : IMetricsService
/// </summary>
internal static class CauNoiHaTang
{
    /// <summary>§2.4 — may trang thai T2..T14. Cai dat phai thuan tuy (khong CSDL).</summary>
    public static INhiemVuStateMachine MayTrangThai() => new NhiemVuStateMachine();

    /// <summary>§6.2 — 15 co quyen. Cai dat phai thuan tuy (khong CSDL).</summary>
    public static IQuyenService Quyen() => new QuyenService();
}

/// <summary>
/// Moi truong CSDL trong bo nho (EF Core InMemory) dung cho cac test can truy van:
/// <c>AiEngineTests</c> (§9) va <c>SeedDataTests</c> (§8 T9 / §9.8).
///
/// Truy van bang <c>DbContext.Set&lt;T&gt;()</c> chu KHONG bang ten thuoc tinh DbSet,
/// de bo test khong phu thuoc cach dat ten DbSet cua tang ha tang.
/// </summary>
internal sealed class MoiTruongDb : IAsyncDisposable
{
    private readonly QlnvDbContext _db;

    private MoiTruongDb()
    {
        var options = new DbContextOptionsBuilder<QlnvDbContext>()
            .UseInMemoryDatabase("qlnv-test-" + Guid.NewGuid().ToString("N"))
            // InMemory khong ho tro giao dich — tat canh bao de log test khong bi nhieu.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                .InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        _db = new QlnvDbContext(options);
        GoiY = new RecommendationService(_db);
        Metrics = new MetricsService(_db);
    }

    /// <summary>§5.8 H1/H2/H4 — dong co goi y nguoi thuc hien.</summary>
    public IRecommendationService GoiY { get; }

    /// <summary>§9.8 — do hieu qua goi y (Precision@1/@3, MRR, Gini...).</summary>
    public IMetricsService Metrics { get; }

    /// <summary>Tao moi truong RONG — test tu bom du lieu bang <see cref="Them"/>.</summary>
    public static MoiTruongDb Rong() => new();

    /// <summary>
    /// Tao moi truong va chay bo du lieu mau (§8 T9: &gt;= 300 nhiem vu lich su,
    /// 5-8 linh vuc, 15-20 nguoi dung).
    /// </summary>
    public static async Task<MoiTruongDb> CoDuLieuMauAsync(CancellationToken ct = default)
    {
        var mt = new MoiTruongDb();
        await DbInitializer.SeedAsync(mt._db, ct);
        return mt;
    }

    /// <summary>Them thuc the vao ngu canh (chua luu).</summary>
    public MoiTruongDb Them(params object[] thucThe)
    {
        _db.AddRange(thucThe);
        return this;
    }

    /// <summary>Ghi cac thuc the da them xuong CSDL trong bo nho.</summary>
    public Task<int> LuuAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    /// <summary>Doc toan bo ban ghi kieu <typeparamref name="T"/> (khong theo doi thay doi).</summary>
    public async Task<IReadOnlyList<T>> DocAsync<T>(CancellationToken ct = default) where T : class
        => await _db.Set<T>().AsNoTracking().ToListAsync(ct);

    /// <summary>Dem so ban ghi kieu <typeparamref name="T"/>.</summary>
    public Task<int> DemAsync<T>(CancellationToken ct = default) where T : class
        => _db.Set<T>().CountAsync(ct);

    /// <summary>Toan bo nhiem vu (§4.2).</summary>
    public Task<IReadOnlyList<DmNhiemVuChiTiet>> NhiemVuAsync(CancellationToken ct = default)
        => DocAsync<DmNhiemVuChiTiet>(ct);

    /// <summary>Toan bo ban ghi phan cong (§4.3).</summary>
    public Task<IReadOnlyList<NhiemVuPhanCong>> PhanCongAsync(CancellationToken ct = default)
        => DocAsync<NhiemVuPhanCong>(ct);

    /// <summary>Toan bo nguoi dung (§4.7).</summary>
    public Task<IReadOnlyList<SysUser>> NguoiDungAsync(CancellationToken ct = default)
        => DocAsync<SysUser>(ct);

    /// <summary>Toan bo nhat ky goi y AI (§4.8).</summary>
    public Task<IReadOnlyList<AiGoiYLog>> NhatKyAiAsync(CancellationToken ct = default)
        => DocAsync<AiGoiYLog>(ct);

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _db.DisposeAsync();
}
