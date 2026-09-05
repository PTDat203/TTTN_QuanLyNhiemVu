using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.1 — anh xa <see cref="DmVanBan"/> sang bang <c>DM_VANBAN</c>.
/// §7.4 muc 1: ten cot giu nguyen chu thuong nhu he goc.
/// </summary>
public sealed class DmVanBanConfiguration : IEntityTypeConfiguration<DmVanBan>
{
    public void Configure(EntityTypeBuilder<DmVanBan> builder)
    {
        builder.ToTable("DM_VANBAN");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.SoKyHieu)
            .HasColumnName("sokyhieu")
            .HasMaxLength(GioiHan.DoDaiSoKyHieu);

        builder.Property(x => x.TrichYeu)
            .HasColumnName("trichyeu")
            .HasMaxLength(GioiHan.DoDaiTrichYeu)
            .IsRequired();

        builder.Property(x => x.LoaiVb)
            .HasColumnName("loaivb")
            .HasMaxLength(50);

        builder.Property(x => x.NgayBanHanh)
            .HasColumnName("ngaybanhanh");

        builder.Property(x => x.CoQuanBanHanh)
            .HasColumnName("coquanbanhanh")
            .HasMaxLength(GioiHan.DoDaiCoQuanBanHanh);

        builder.Property(x => x.DoKhan)
            .HasColumnName("dokhan")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LinhVuc)
            .HasColumnName("linhvuc")
            .HasMaxLength(50);

        builder.Property(x => x.ThoiGianChiDao)
            .HasColumnName("thoigianchidao");

        builder.Property(x => x.NguonNv)
            .HasColumnName("nguonnv")
            .HasMaxLength(200);

        builder.Property(x => x.NguoiTheoDoi)
            .HasColumnName("nguoitheodoi")
            .HasMaxLength(GioiHan.DoDaiCoQuanBanHanh);

        builder.Property(x => x.UnitCode)
            .HasColumnName("unitcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UserIdCreate)
            .HasColumnName("useridcreate")
            .IsRequired();

        builder.Property(x => x.CreateDate)
            .HasColumnName("createdate")
            .IsRequired();

        // BO SUNG ngoai §4.1 — can cho luoi M03 sap theo lan sua cuoi
        builder.Property(x => x.UpdateDate)
            .HasColumnName("updatedate");

        // §8 T12 — chi muc phuc vu loc/ sap xep tren man M03
        builder.HasIndex(x => x.SoKyHieu).HasDatabaseName("IX_DM_VANBAN_sokyhieu");
        builder.HasIndex(x => x.NgayBanHanh).HasDatabaseName("IX_DM_VANBAN_ngaybanhanh");
        builder.HasIndex(x => x.LinhVuc).HasDatabaseName("IX_DM_VANBAN_linhvuc");
        builder.HasIndex(x => x.UnitCode).HasDatabaseName("IX_DM_VANBAN_unitcode");

        builder.HasMany(x => x.NhiemVuChiTiet)
            .WithOne(x => x.VanBan)
            .HasForeignKey(x => x.IdVb)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
