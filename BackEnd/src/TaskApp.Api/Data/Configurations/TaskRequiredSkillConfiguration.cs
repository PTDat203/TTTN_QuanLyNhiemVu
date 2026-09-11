using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>Ánh xạ bảng TASK_REQUIRED_SKILLS.</summary>
public class TaskRequiredSkillConfiguration : IEntityTypeConfiguration<TaskRequiredSkill>
{
    public void Configure(EntityTypeBuilder<TaskRequiredSkill> e)
    {
        e.ToTable("TASK_REQUIRED_SKILLS");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        e.Property(x => x.TaskId).HasColumnName("TASK_ID").IsRequired();
        e.Property(x => x.SkillId).HasColumnName("SKILL_ID").IsRequired();
        e.Property(x => x.RequiredLevel).HasColumnName("REQUIRED_LEVEL");
        e.Property(x => x.Source).HasColumnName("SOURCE").HasMaxLength(10).IsRequired();
        e.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");

        // Xoá nhiệm vụ thì xoá luôn kỹ năng yêu cầu — khớp ON DELETE CASCADE trong database.
        // Ứng dụng có xoá nhiệm vụ MOI_TAO, đúng lúc AI vừa trích kỹ năng xong.
        e.HasOne(x => x.Task)
            .WithMany(t => t.KyNangYeuCau)
            .HasForeignKey(x => x.TaskId)
            .HasConstraintName("FK_TASK_REQ_SKILLS_TASK")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Skill)
            .WithMany(s => s.NhiemVuCan)
            .HasForeignKey(x => x.SkillId)
            .HasConstraintName("FK_TASK_REQ_SKILLS_SKILL")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.TaskId, x.SkillId }).IsUnique().HasDatabaseName("UQ_TASK_REQ_SKILL");
        e.HasIndex(x => x.SkillId).HasDatabaseName("IDX_TASK_REQ_SKILLS_SKILL_ID");
    }
}
