using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="TaskReport"/> vao bang TASK_REPORTS.
/// <para>
/// Bang nay cung co HAI khoa ngoai tro ve USERS (REPORTER_ID va REVIEWER_ID),
/// nen phai chi ro <c>HasForeignKey</c> cho tung quan he giong nhu bang TASKS.
/// </para>
/// </summary>
public class TaskReportConfiguration : IEntityTypeConfiguration<TaskReport>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="TaskReport"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<TaskReport> e)
    {
        e.ToTable("TASK_REPORTS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.TaskId)
            .HasColumnName("TASK_ID")
            .IsRequired();

        e.Property(x => x.ReporterId)
            .HasColumnName("REPORTER_ID")
            .IsRequired();

        e.Property(x => x.Content)
            .HasColumnName("CONTENT")
            .HasColumnType("CLOB")
            .IsRequired();

        e.Property(x => x.Status)
            .HasColumnName("REPORT_STATUS")
            .HasMaxLength(30)
            .IsRequired();

        e.Property(x => x.ReviewerId)
            .HasColumnName("REVIEWER_ID");

        e.Property(x => x.ReviewNote)
            .HasColumnName("REVIEW_NOTE")
            .HasColumnType("CLOB");

        e.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT");

        e.Property(x => x.ReviewedAt)
            .HasColumnName("REVIEWED_AT");

        e.HasOne(x => x.Task)
            .WithMany(t => t.Reports)
            .HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_REPORT_TASK")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Reporter)
            .WithMany(u => u.BaoCaoDaGui)
            .HasForeignKey(x => x.ReporterId)
            .HasConstraintName("FK_REPORT_REPORTER")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Reviewer)
            .WithMany(u => u.BaoCaoDaDuyet)
            .HasForeignKey(x => x.ReviewerId)
            .HasConstraintName("FK_REPORT_REVIEWER")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.TaskId).HasDatabaseName("IDX_TASK_REPORTS_TASK_ID");
        e.HasIndex(x => x.ReporterId).HasDatabaseName("IDX_TASK_REPORTS_REPORTER_ID");
        e.HasIndex(x => x.ReviewerId).HasDatabaseName("IDX_TASK_REPORTS_REVIEWER_ID");
    }
}
