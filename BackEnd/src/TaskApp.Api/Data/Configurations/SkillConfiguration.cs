using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>Ánh xạ bảng SKILLS.</summary>
public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> e)
    {
        e.ToTable("SKILLS");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        e.Property(x => x.Code).HasColumnName("CODE").HasMaxLength(50).IsRequired();
        e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(150).IsRequired();
        e.Property(x => x.Category).HasColumnName("CATEGORY").HasMaxLength(30).IsRequired();
        e.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
        e.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");

        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_SKILLS_CODE");
        e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_SKILLS_NAME");
    }
}
