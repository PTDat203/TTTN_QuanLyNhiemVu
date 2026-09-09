using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa bang TASK_ATTACHMENTS.
///
/// Bang nay co BA khoa ngoai (TASK_ID, REPORT_ID, UPLOADED_BY) nen phai chi ro
/// <c>HasForeignKey</c> cho tung quan he, neu khong EF Core khong suy duoc va bao loi
/// quan he mo ho luc khoi tao model.
///
/// REPORT_ID cho phep NULL — day la cach phan biet hai loai tep:
///   TASK_ID co gia tri, REPORT_ID NULL  -> tep dinh kem truc tiep vao nhiem vu
///   TASK_ID co gia tri, REPORT_ID co    -> tep thuoc mot ban bao cao cu the
/// Database chi luu duong dan, khong luu binary.
/// </summary>
public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> e)
    {
        e.ToTable("TASK_ATTACHMENTS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.TaskId)
            .HasColumnName("TASK_ID")
            .IsRequired();

        e.Property(x => x.ReportId)
            .HasColumnName("REPORT_ID");

        e.Property(x => x.FileName)
            .HasColumnName("FILE_NAME")
            .HasMaxLength(255)
            .IsRequired();

        e.Property(x => x.FilePath)
            .HasColumnName("FILE_PATH")
            .HasMaxLength(500)
            .IsRequired();

        e.Property(x => x.FileType)
            .HasColumnName("FILE_TYPE")
            .HasMaxLength(100);

        e.Property(x => x.FileSize)
            .HasColumnName("FILE_SIZE");

        e.Property(x => x.UploadedBy)
            .HasColumnName("UPLOADED_BY")
            .IsRequired();

        e.Property(x => x.UploadedAt)
            .HasColumnName("UPLOADED_AT");

        // TASKS 1 ----- N TASK_ATTACHMENTS
        e.HasOne(x => x.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TASK_ATTACHMENTS_TASK")
            .OnDelete(DeleteBehavior.Restrict);

        // TASK_REPORTS 1 ----- N TASK_ATTACHMENTS (REPORT_ID nullable)
        e.HasOne(x => x.Report)
            .WithMany(r => r.Attachments)
            .HasForeignKey(x => x.ReportId)
            .HasConstraintName("FK_TASK_ATTACHMENTS_REPORT")
            .OnDelete(DeleteBehavior.Restrict);

        // USERS 1 ----- N TASK_ATTACHMENTS (nguoi tai len)
        e.HasOne(x => x.Uploader)
            .WithMany(u => u.TepDaTaiLen)
            .HasForeignKey(x => x.UploadedBy)
            .HasConstraintName("FK_TASK_ATTACHMENTS_USER")
            .OnDelete(DeleteBehavior.Restrict);

        // Ba index nay co that trong DB
        e.HasIndex(x => x.TaskId).HasDatabaseName("IDX_TASK_ATTACHMENTS_TASK_ID");
        e.HasIndex(x => x.ReportId).HasDatabaseName("IDX_TASK_ATTACHMENTS_REPORT_ID");
        e.HasIndex(x => x.UploadedBy).HasDatabaseName("IDX_TASK_ATTACHMENTS_UPLOADED_BY");
    }
}
