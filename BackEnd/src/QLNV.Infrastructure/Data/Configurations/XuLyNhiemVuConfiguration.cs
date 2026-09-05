using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.4 — anh xa <see cref="XuLyNhiemVu"/> sang bang <c>XULY_NHIEMVU</c>.
///
/// Cot <c>trangthaiDvXuly</c> la BO SUNG ngoai §4.4: §4.8 dem
/// <c>so_nv_bi_tralai</c> tu chinh bang nay voi truc B = 12, ma §4.4 lai khong khai cot do.
/// </summary>
public sealed class XuLyNhiemVuConfiguration : IEntityTypeConfiguration<XuLyNhiemVu>
{
    public void Configure(EntityTypeBuilder<XuLyNhiemVu> builder)
    {
        builder.ToTable("XULY_NHIEMVU");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IdCtnv)
            .HasColumnName("idCtnv")
            .IsRequired();

        builder.Property(x => x.Loai)
            .HasColumnName("loai")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.NoiDung)
            .HasColumnName("noidung")
            .HasMaxLength(GioiHan.DoDaiNoiDung);

        builder.Property(x => x.MucDoHt)
            .HasColumnName("mucdoht");

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai");

        builder.Property(x => x.TrangThaiXuLy)
            .HasColumnName("trangthaiXuly");

        builder.Property(x => x.TrangThaiDvXuly)
            .HasColumnName("trangthaiDvXuly");

        builder.Property(x => x.UserIdXuLy)
            .HasColumnName("useridXuly")
            .IsRequired();

        builder.Property(x => x.NgayXuLy)
            .HasColumnName("ngayxuly")
            .IsRequired();

        // §8 T12 — tab "Lich su xu ly" (M07/M09) sap theo ngayxuly cua tung nhiem vu
        builder.HasIndex(x => new { x.IdCtnv, x.NgayXuLy })
            .HasDatabaseName("IX_XULY_NHIEMVU_idCtnv_ngayxuly");

        // §9.4 S3(b) — dem so lan bi tra lai theo nguoi xu ly
        builder.HasIndex(x => x.UserIdXuLy).HasDatabaseName("IX_XULY_NHIEMVU_useridXuly");
        builder.HasIndex(x => x.TrangThaiDvXuly).HasDatabaseName("IX_XULY_NHIEMVU_trangthaiDvXuly");
        builder.HasIndex(x => x.Loai).HasDatabaseName("IX_XULY_NHIEMVU_loai");

        // Quan he toi DM_NHIEMVU_CHITIET da khai o DmNhiemVuChiTietConfiguration.
        builder.HasOne(x => x.NguoiXuLy)
            .WithMany()
            .HasForeignKey(x => x.UserIdXuLy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
