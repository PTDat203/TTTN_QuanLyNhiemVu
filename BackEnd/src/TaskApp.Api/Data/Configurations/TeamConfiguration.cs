using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>Ánh xạ bảng TEAMS.</summary>
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> e)
    {
        e.ToTable("TEAMS");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        e.Property(x => x.DepartmentId).HasColumnName("DEPARTMENT_ID").IsRequired();
        e.Property(x => x.Code).HasColumnName("CODE").HasMaxLength(30).IsRequired();
        e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
        e.Property(x => x.LeaderUserId).HasColumnName("LEADER_USER_ID");
        e.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
        e.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");

        e.HasOne(x => x.Department)
            .WithMany(d => d.Nhom)
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_TEAMS_DEPARTMENT")
            .OnDelete(DeleteBehavior.Restrict);

        // User trỏ tới Team bằng hai đường: là thành viên (USERS.TEAM_ID) và là trưởng
        // nhóm (TEAMS.LEADER_USER_ID). Đường thứ hai không khai navigation ngược, nếu không
        // EF Core không biết ghép navigation nào với khoá ngoại nào.
        e.HasOne(x => x.Leader)
            .WithMany()
            .HasForeignKey(x => x.LeaderUserId)
            .HasConstraintName("FK_TEAMS_LEADER")
            .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique().HasDatabaseName("UQ_TEAMS_DEPT_CODE");
        e.HasIndex(x => x.LeaderUserId).HasDatabaseName("IDX_TEAMS_LEADER_USER_ID");
    }
}
