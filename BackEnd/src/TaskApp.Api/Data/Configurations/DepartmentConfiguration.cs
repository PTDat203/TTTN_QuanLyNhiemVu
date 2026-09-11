using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>Ánh xạ bảng DEPARTMENTS.</summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> e)
    {
        e.ToTable("DEPARTMENTS");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        e.Property(x => x.Code).HasColumnName("CODE").HasMaxLength(30).IsRequired();
        e.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
        e.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");

        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_DEPARTMENTS_CODE");
    }
}
