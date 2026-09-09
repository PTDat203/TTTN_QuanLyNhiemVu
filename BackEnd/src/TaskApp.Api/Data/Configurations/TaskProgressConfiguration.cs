using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="TaskProgress"/> vao bang TASK_PROGRESS.
/// Bang chi co CREATED_AT, khong co UPDATED_AT.
/// </summary>
public class TaskProgressConfiguration : IEntityTypeConfiguration<TaskProgress>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="TaskProgress"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<TaskProgress> e)
    {
        e.ToTable("TASK_PROGRESS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.TaskId)
            .HasColumnName("TASK_ID")
            .IsRequired();

        e.Property(x => x.UserId)
            .HasColumnName("USER_ID")
            .IsRequired();

        // Rang buoc 0..100 do CK_PROGRESS_PERCENT giu o phia database;
        // tang Services kiem tra lai truoc khi ghi de tra thong bao tieng Viet ro rang.
        e.Property(x => x.ProgressPercent)
            .HasColumnName("PROGRESS_PERCENT")
            .IsRequired();

        e.Property(x => x.Content)
            .HasColumnName("CONTENT")
            .HasMaxLength(2000);

        e.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT");

        e.HasOne(x => x.Task)
            .WithMany(t => t.ProgressUpdates)
            .HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_PROGRESS_TASK")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.User)
            .WithMany(u => u.CapNhatTienDo)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_PROGRESS_USER")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.TaskId).HasDatabaseName("IDX_TASK_PROGRESS_TASK_ID");
        e.HasIndex(x => x.UserId).HasDatabaseName("IDX_TASK_PROGRESS_USER_ID");
    }
}
