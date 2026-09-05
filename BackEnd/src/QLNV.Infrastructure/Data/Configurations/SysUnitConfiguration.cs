using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.7 — anh xa <see cref="SysUnit"/> sang bang <c>SYS_UNIT</c>.
/// Khoa chinh la CHUOI <c>unitcode</c> (bam he goc), tu tham chieu qua <c>macha</c>.
/// </summary>
public sealed class SysUnitConfiguration : IEntityTypeConfiguration<SysUnit>
{
    public void Configure(EntityTypeBuilder<SysUnit> builder)
    {
        builder.ToTable("SYS_UNIT");

        builder.HasKey(x => x.UnitCode);

        builder.Property(x => x.UnitCode)
            .HasColumnName("unitcode")
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(x => x.TenDonVi)
            .HasColumnName("tendonvi")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.MaCha)
            .HasColumnName("macha")
            .HasMaxLength(50);

        builder.Property(x => x.CapDonVi)
            .HasColumnName("capdonvi")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .IsRequired();

        builder.HasIndex(x => x.MaCha).HasDatabaseName("IX_SYS_UNIT_macha");

        builder.HasOne(x => x.DonViCha)
            .WithMany(x => x.DonViCon)
            .HasForeignKey(x => x.MaCha)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
