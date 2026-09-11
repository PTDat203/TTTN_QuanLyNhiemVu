using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="TaskItem"/> vao bang TASKS.
/// <para>
/// Bang nay co HAI khoa ngoai cung tro ve USERS (CREATOR_ID va ASSIGNEE_ID).
/// Bat buoc chi ro <c>HasForeignKey</c> va navigation nguoc cho tung quan he,
/// neu khong EF Core khong biet ghep cap navigation nao voi khoa ngoai nao
/// va se bao loi quan he mo ho luc dung mo hinh.
/// </para>
/// </summary>
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="TaskItem"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<TaskItem> e)
    {
        e.ToTable("TASKS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.Title)
            .HasColumnName("TITLE")
            .HasMaxLength(200)
            .IsRequired();

        // CLOB khong co gioi han do dai nen KHONG goi HasMaxLength
        e.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasColumnType("CLOB");

        e.Property(x => x.CreatorId)
            .HasColumnName("CREATOR_ID")
            .IsRequired();

        e.Property(x => x.AssigneeId)
            .HasColumnName("ASSIGNEE_ID");

        e.Property(x => x.Priority)
            .HasColumnName("PRIORITY")
            .HasMaxLength(20)
            .IsRequired();

        e.Property(x => x.StatusCode)
            .HasColumnName("STATUS_CODE")
            .HasMaxLength(30)
            .IsRequired();

        // Cot Oracle kieu DATE. Dung DateTime, KHONG dung DateOnly -
        // provider Oracle 8.23.x chua anh xa duoc DateOnly/TimeOnly.
        e.Property(x => x.StartDate)
            .HasColumnName("START_DATE")
            .HasColumnType("DATE");

        e.Property(x => x.DueDate)
            .HasColumnName("DUE_DATE")
            .HasColumnType("DATE");

        e.Property(x => x.DepartmentId)
            .HasColumnName("DEPARTMENT_ID");

        e.Property(x => x.TeamId)
            .HasColumnName("TEAM_ID");

        e.Property(x => x.EstimatedEffort)
            .HasColumnName("ESTIMATED_EFFORT")
            .HasPrecision(5, 1);

        e.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT");

        e.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT");

        // Quan he 1: nguoi tao/giao nhiem vu
        e.HasOne(x => x.Creator)
            .WithMany(u => u.NhiemVuDaTao)
            .HasForeignKey(x => x.CreatorId)
            .HasConstraintName("FK_TASK_CREATOR")
            .OnDelete(DeleteBehavior.Restrict);

        // Quan he 2: nguoi thuc hien (cho phep rong khi nhiem vu con o MOI_TAO)
        e.HasOne(x => x.Assignee)
            .WithMany(u => u.NhiemVuDuocGiao)
            .HasForeignKey(x => x.AssigneeId)
            .HasConstraintName("FK_TASK_ASSIGNEE")
            .OnDelete(DeleteBehavior.Restrict);

        // Quan he 3: danh muc trang thai nhiem vu
        e.HasOne(x => x.Status)
            .WithMany(s => s.Tasks)
            .HasForeignKey(x => x.StatusCode)
            .HasPrincipalKey(s => s.Code)
            .HasConstraintName("FK_TASK_STATUS")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.AssigneeId).HasDatabaseName("IDX_TASKS_ASSIGNEE_ID");
        e.HasIndex(x => x.CreatorId).HasDatabaseName("IDX_TASKS_CREATOR_ID");
        e.HasIndex(x => x.StatusCode).HasDatabaseName("IDX_TASKS_STATUS_CODE");
    
        // TASKS n ----- 1 DEPARTMENTS
        e.HasOne(x => x.Department)
            .WithMany(d => d.NhiemVu)
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_TASKS_DEPARTMENT")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.DepartmentId).HasDatabaseName("IDX_TASKS_DEPARTMENT_ID");

        // TASKS n ----- 1 TEAMS. Như USERS, database có thêm khoá ngoại ghép
        // FK_TASKS_TEAM_DEPT để nhóm phụ trách không lệch khỏi phòng thực thi.
        e.HasOne(x => x.Team)
            .WithMany(t => t.NhiemVu)
            .HasForeignKey(x => x.TeamId)
            .HasConstraintName("FK_TASKS_TEAM")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.TeamId, x.DepartmentId }).HasDatabaseName("IDX_TASKS_TEAM_DEPT");
}
}
