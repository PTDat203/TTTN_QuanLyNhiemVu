using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="TaskStatusLookup"/> vao bang TASK_STATUS_LOOKUP.
/// Khoa chinh la chuoi CODE do ung dung tu dat, KHONG sinh tu dong -
/// bat buoc goi <c>ValueGeneratedNever()</c>.
/// </summary>
public class TaskStatusLookupConfiguration : IEntityTypeConfiguration<TaskStatusLookup>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="TaskStatusLookup"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<TaskStatusLookup> e)
    {
        e.ToTable("TASK_STATUS_LOOKUP");

        e.HasKey(x => x.Code);

        e.Property(x => x.Code)
            .HasColumnName("CODE")
            .HasMaxLength(30)
            .ValueGeneratedNever()
            .IsRequired();

        e.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(255);

        e.Property(x => x.SortOrder)
            .HasColumnName("SORT_ORDER");
    }
}
