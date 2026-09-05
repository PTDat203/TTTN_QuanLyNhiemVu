using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §7.4 muc 3 — anh xa <see cref="DmTuDien"/> sang bang <c>DM_TUDIEN</c>.
/// Danh muc trang thai BAT BUOC nam o day voi 2 ma type <c>TRANGTHAINV</c> /
/// <c>TRANGTHAIPH</c> de doi nhan ma khong phai build lai (§10.5).
/// Cap (<c>type</c>, <c>ma</c>) la DUY NHAT.
/// </summary>
public sealed class DmTuDienConfiguration : IEntityTypeConfiguration<DmTuDien>
{
    public void Configure(EntityTypeBuilder<DmTuDien> builder)
    {
        builder.ToTable("DM_TUDIEN");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Ma)
            .HasColumnName("ma")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Nhan)
            .HasColumnName("nhan")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Mau)
            .HasColumnName("mau")
            .HasMaxLength(50);

        builder.Property(x => x.MoTa)
            .HasColumnName("mota")
            .HasMaxLength(500);

        builder.Property(x => x.ThuTu)
            .HasColumnName("thutu")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .IsRequired();

        builder.HasIndex(x => new { x.Type, x.Ma })
            .IsUnique()
            .HasDatabaseName("UX_DM_TUDIEN_type_ma");
    }
}
