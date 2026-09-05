using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// Anh xa <see cref="DmLinhVuc"/> sang bang <c>DM_LINHVUC</c>.
/// <c>nhomcha</c> la dau vao cua §9.4 S1 (nhanh chiet khau 50% khi ung vien chua
/// tung lam dung linh vuc nhung da lam linh vuc ANH EM cung nhom cha).
/// </summary>
public sealed class DmLinhVucConfiguration : IEntityTypeConfiguration<DmLinhVuc>
{
    public void Configure(EntityTypeBuilder<DmLinhVuc> builder)
    {
        builder.ToTable("DM_LINHVUC");

        builder.HasKey(x => x.Ma);

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasMaxLength(50)
            .ValueGeneratedNever();

        builder.Property(x => x.Ten)
            .HasColumnName("ten")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.NhomCha)
            .HasColumnName("nhomcha")
            .HasMaxLength(50);

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .IsRequired();

        builder.Property(x => x.ThuTu)
            .HasColumnName("thutu")
            .IsRequired();

        // Thuoc tinh TINH — khong phai cot CSDL
        builder.Ignore(x => x.LaNhomGoc);

        builder.HasIndex(x => x.NhomCha).HasDatabaseName("IX_DM_LINHVUC_nhomcha");

        builder.HasOne(x => x.LinhVucCha)
            .WithMany(x => x.LinhVucCon)
            .HasForeignKey(x => x.NhomCha)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
