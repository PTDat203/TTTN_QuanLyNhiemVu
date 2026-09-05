using Microsoft.EntityFrameworkCore;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data;

/// <summary>
/// DbContext cua phan he Giao nhiem vu (§4 Mo hinh du lieu).
///
/// §7.4 muc 1 — ten BANG va ten COT giu nguyen quy uoc he goc (DM_, _CHITIET,
/// chu thuong khong dau); anh xa nam trong cac lop
/// <c>Data/Configurations/*Configuration.cs</c>, KHONG dat attribute tren entity
/// (QLNV.Core khong duoc phu thuoc EF Core).
///
/// Provider chon luc chay: SQLite (mac dinh khi phat trien) hoac SQL Server —
/// xem <see cref="DependencyInjection.AddInfrastructure"/>.
/// </summary>
public class QlnvDbContext : DbContext
{
    public QlnvDbContext(DbContextOptions<QlnvDbContext> options)
        : base(options)
    {
    }

    /// <summary>§4.1 — bang <c>DM_VANBAN</c>.</summary>
    public DbSet<DmVanBan> VanBan => Set<DmVanBan>();

    /// <summary>§4.2 — bang <c>DM_NHIEMVU_CHITIET</c>.</summary>
    public DbSet<DmNhiemVuChiTiet> NhiemVu => Set<DmNhiemVuChiTiet>();

    /// <summary>§4.3 — bang <c>NHIEMVU_PHANCONG</c>.</summary>
    public DbSet<NhiemVuPhanCong> PhanCong => Set<NhiemVuPhanCong>();

    /// <summary>§4.4 — bang <c>XULY_NHIEMVU</c>.</summary>
    public DbSet<XuLyNhiemVu> XuLy => Set<XuLyNhiemVu>();

    /// <summary>§4.5 — bang <c>GIAHAN_NHIEMVU</c>.</summary>
    public DbSet<GiaHanNhiemVu> GiaHan => Set<GiaHanNhiemVu>();

    /// <summary>§4.6 — bang <c>NHIEMVU_FILE</c>.</summary>
    public DbSet<NhiemVuFile> Files => Set<NhiemVuFile>();

    /// <summary>§4.7 — bang <c>SYS_USER</c>.</summary>
    public DbSet<SysUser> NguoiDung => Set<SysUser>();

    /// <summary>§4.7 — bang <c>SYS_UNIT</c>.</summary>
    public DbSet<SysUnit> DonVi => Set<SysUnit>();

    /// <summary>Bang <c>DM_LINHVUC</c> — danh muc linh vuc co phan cap (§9.4 S1).</summary>
    public DbSet<DmLinhVuc> LinhVuc => Set<DmLinhVuc>();

    /// <summary>Bang <c>DM_TUDIEN</c> — danh muc ma-&gt;nhan (§7.4 muc 3).</summary>
    public DbSet<DmTuDien> TuDien => Set<DmTuDien>();

    /// <summary>§4.8 — bang <c>AI_GOIY_LOG</c>.</summary>
    public DbSet<AiGoiYLog> AiGoiYLog => Set<AiGoiYLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Nap toan bo IEntityTypeConfiguration<> trong chinh assembly nay
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QlnvDbContext).Assembly);
    }
}
