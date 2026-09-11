using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data;

/// <summary>
/// DbContext cua mini app Quan ly nhiem vu.
///
/// NGUON CHUAN LA DATABASE ORACLE DANG CHAY, khong phai class nay.
/// Schema TASK_APP da duoc tao san bang script SQL chay tay. Vi vay:
///   - KHONG goi EnsureCreated(), KHONG goi Migrate().
///   - Khong co thu muc Migrations.
///   - Moi thay doi schema lam truc tiep tren Oracle roi moi sua lai anh xa o day.
///
/// Oracle viet hoa moi dinh danh khong dat trong nhay kep, con EF Core thi LUON dat
/// dinh danh trong nhay kep. Vi vay moi bang va moi cot deu phai map VIET HOA tuong minh
/// trong cac lop IEntityTypeConfiguration — thieu mot cot la cot do sinh ra "PropertyName"
/// va bao ORA-00904: invalid identifier.
/// </summary>
public class TaskDbContext : DbContext
{
    /// <summary>Ten schema chu so huu 7 bang.</summary>
    public const string Schema = "TASK_APP";

    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserQualification> UserQualifications => Set<UserQualification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<TaskStatusLookup> TaskStatusLookups => Set<TaskStatusLookup>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskProgress> TaskProgresses => Set<TaskProgress>();
    public DbSet<TaskReport> TaskReports => Set<TaskReport>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Moi bang thuoc schema TASK_APP, khong phai schema mac dinh cua nguoi dang nhap
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Tu dat CREATED_AT / UPDATED_AT.
    ///
    /// DDL co DEFAULT CURRENT_TIMESTAMP nhung Oracle CHI ap dung default luc INSERT.
    /// Khi UPDATE, Oracle khong tu dong dung vao cot nao ca — neu khong xu ly o day thi
    /// UPDATED_AT se dung yen mai o thoi diem tao.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        CapNhatMocThoiGian();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        CapNhatMocThoiGian();
        return base.SaveChanges();
    }

    private void CapNhatMocThoiGian()
    {
        var bayGio = DateTime.Now;

        foreach (var muc in ChangeTracker.Entries<ICoNgayTao>())
        {
            if (muc.State == EntityState.Added && muc.Entity.CreatedAt == default)
            {
                muc.Entity.CreatedAt = bayGio;
            }
        }

        foreach (var muc in ChangeTracker.Entries<IAuditable>())
        {
            if (muc.State is EntityState.Added or EntityState.Modified)
            {
                muc.Entity.UpdatedAt = bayGio;
            }
        }
    }
}
