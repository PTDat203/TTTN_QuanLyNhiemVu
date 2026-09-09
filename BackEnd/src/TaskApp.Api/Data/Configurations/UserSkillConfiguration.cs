using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="UserSkill"/> vao bang USER_SKILLS.
/// </summary>
public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="UserSkill"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<UserSkill> e)
    {
        e.ToTable("USER_SKILLS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.UserId)
            .HasColumnName("USER_ID")
            .IsRequired();

        e.Property(x => x.SkillName)
            .HasColumnName("SKILL_NAME")
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.SkillLevel)
            .HasColumnName("SKILL_LEVEL");

        e.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(500);

        e.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT");

        e.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT");

        // USERS 1 ----- N USER_SKILLS
        e.HasOne(x => x.User)
            .WithMany(u => u.Skills)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_USER_SKILLS_USER")
            .OnDelete(DeleteBehavior.Restrict);

        // Trung voi rang buoc UQ_USER_SKILL trong DDL: moi nguoi khong khai trung ten ky nang
        e.HasIndex(x => new { x.UserId, x.SkillName })
            .IsUnique()
            .HasDatabaseName("UQ_USER_SKILL");

        // IDX_USER_SKILLS_USER_ID co that trong DB. AI goi y quet ky nang theo tung nguoi
        // nen day la duong truy cap nong nhat cua bang nay.
        e.HasIndex(x => x.UserId)
            .HasDatabaseName("IDX_USER_SKILLS_USER_ID");
    }
}
