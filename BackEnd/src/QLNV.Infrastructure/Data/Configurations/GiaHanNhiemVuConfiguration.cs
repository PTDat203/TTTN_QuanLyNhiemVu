using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.5 — anh xa <see cref="GiaHanNhiemVu"/> sang bang <c>GIAHAN_NHIEMVU</c>.
/// Cot <c>trangthai_cu</c> la BO SUNG (ban flow.js da kiem chung): nho truc A ngay
/// TRUOC khi chuyen sang 13 de T12/T13 khoi phuc dung (§2.4).
/// </summary>
public sealed class GiaHanNhiemVuConfiguration : IEntityTypeConfiguration<GiaHanNhiemVu>
{
    public void Configure(EntityTypeBuilder<GiaHanNhiemVu> builder)
    {
        builder.ToTable("GIAHAN_NHIEMVU");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IdCtnv)
            .HasColumnName("idCtnv")
            .IsRequired();

        builder.Property(x => x.NoiDung)
            .HasColumnName("noiDung")
            .HasMaxLength(GioiHan.DoDaiNoiDung);

        builder.Property(x => x.HanXuLyDeXuat)
            .HasColumnName("hanxulydexuat")
            .IsRequired();

        builder.Property(x => x.HanXuLyThCu)
            .HasColumnName("hanxulyth_cu");

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangThai")
            .IsRequired();

        builder.Property(x => x.PhanHoi)
            .HasColumnName("phanhoi")
            .HasMaxLength(GioiHan.DoDaiNoiDung);

        builder.Property(x => x.UserIdDeXuat)
            .HasColumnName("useridDexuat")
            .IsRequired();

        builder.Property(x => x.UserIdDuyet)
            .HasColumnName("useridDuyet");

        builder.Property(x => x.CreateDate)
            .HasColumnName("createDate")
            .IsRequired();

        builder.Property(x => x.NgayDuyet)
            .HasColumnName("ngayduyet");

        builder.Property(x => x.TrangThaiCu)
            .HasColumnName("trangthai_cu");

        // Thuoc tinh TINH — khong phai cot CSDL
        builder.Ignore(x => x.DangChoDuyet);

        // §8 T12 — man M12 loc de xuat dang cho duyet (trangThai = 10)
        builder.HasIndex(x => new { x.IdCtnv, x.TrangThai })
            .HasDatabaseName("IX_GIAHAN_NHIEMVU_idCtnv_trangThai");
        builder.HasIndex(x => x.UserIdDeXuat).HasDatabaseName("IX_GIAHAN_NHIEMVU_useridDexuat");

        // Quan he toi DM_NHIEMVU_CHITIET da khai o DmNhiemVuChiTietConfiguration.
    }
}
