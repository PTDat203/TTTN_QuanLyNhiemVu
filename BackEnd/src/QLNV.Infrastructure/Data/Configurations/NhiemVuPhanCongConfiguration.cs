using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.3 — anh xa <see cref="NhiemVuPhanCong"/> sang bang <c>NHIEMVU_PHANCONG</c>.
///
/// Chi muc DUY NHAT <c>(idnvchitiet, userid)</c> ep dung rang buoc §4.3:
/// "mot userid khong duoc vua CHUTRI vua PHOIHOP tren cung mot nhiem vu".
/// He qua co chu dinh: khi thu hoi phan cong (§5.3 C7, <c>trangthai = 0</c>) roi giao
/// lai chinh nguoi do, tang nghiep vu phai BAT LAI ban ghi cu (dat <c>trangthai = 1</c>)
/// chu KHONG duoc chen ban ghi thu hai.
/// </summary>
public sealed class NhiemVuPhanCongConfiguration : IEntityTypeConfiguration<NhiemVuPhanCong>
{
    public void Configure(EntityTypeBuilder<NhiemVuPhanCong> builder)
    {
        builder.ToTable("NHIEMVU_PHANCONG");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IdNvChiTiet)
            .HasColumnName("idnvchitiet")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("userid")
            .IsRequired();

        builder.Property(x => x.UnitCode)
            .HasColumnName("unitcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VaiTro)
            .HasColumnName("vaitro")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.UserIdCreate)
            .HasColumnName("useridCreate")
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .IsRequired();

        builder.Property(x => x.CreateDate)
            .HasColumnName("createdate")
            .IsRequired();

        // Thuoc tinh TINH — khong phai cot CSDL
        builder.Ignore(x => x.LaChuTriConHieuLuc);
        builder.Ignore(x => x.LaPhoiHopConHieuLuc);

        // §4.3 — mot nguoi chi co DUNG MOT ban ghi phan cong tren mot nhiem vu
        builder.HasIndex(x => new { x.IdNvChiTiet, x.UserId })
            .IsUnique()
            .HasDatabaseName("UX_NHIEMVU_PHANCONG_idnvchitiet_userid");

        // §8 T12 — truy van "viec cua toi" (§5.3 C3 vaiTro = TOI_LAM) di theo userid
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_NHIEMVU_PHANCONG_userid");
        builder.HasIndex(x => new { x.UserId, x.VaiTro, x.TrangThai })
            .HasDatabaseName("IX_NHIEMVU_PHANCONG_userid_vaitro_trangthai");

        // Quan he toi DM_NHIEMVU_CHITIET da khai o DmNhiemVuChiTietConfiguration.
        builder.HasOne(x => x.NguoiDung)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
