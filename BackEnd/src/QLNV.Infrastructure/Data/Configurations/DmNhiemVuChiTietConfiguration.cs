using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.2 — anh xa <see cref="DmNhiemVuChiTiet"/> sang bang <c>DM_NHIEMVU_CHITIET</c>.
///
/// §7.4 muc 1 — cac ten cot BAT BUOC giu nguyen:
/// <c>trangthai</c>, <c>trangthaiDvXuly</c>, <c>trangthaixulygiahan</c>, <c>solangiahan</c>,
/// <c>mucdoht</c>, <c>hanxulyth</c>, <c>hanxulyph</c>, <c>noidung</c>, <c>phanhoi</c>,
/// <c>dokhan</c>, <c>linhvuc</c>, <c>hsChatluong</c>, <c>idvb</c>, <c>userIdGiaoViec</c>.
/// </summary>
public sealed class DmNhiemVuChiTietConfiguration : IEntityTypeConfiguration<DmNhiemVuChiTiet>
{
    public void Configure(EntityTypeBuilder<DmNhiemVuChiTiet> builder)
    {
        builder.ToTable("DM_NHIEMVU_CHITIET");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IdVb)
            .HasColumnName("idvb")
            .IsRequired();

        builder.Property(x => x.NoiDung)
            .HasColumnName("noidung")
            .HasMaxLength(GioiHan.DoDaiNoiDung)
            .IsRequired();

        builder.Property(x => x.LinhVuc)
            .HasColumnName("linhvuc")
            .HasMaxLength(50);

        builder.Property(x => x.DoKhan)
            .HasColumnName("dokhan")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.HanXuLyTh)
            .HasColumnName("hanxulyth");

        builder.Property(x => x.SoNgayHxlTh)
            .HasColumnName("songayhxlth");

        builder.Property(x => x.HanXuLyPh)
            .HasColumnName("hanxulyph");

        builder.Property(x => x.NgayGiao)
            .HasColumnName("ngaygiao")
            .IsRequired();

        // MOI so voi he goc (§1.1, §10.3) — moc tiep nhan nhiem vu (§2.4 T2)
        builder.Property(x => x.NgayTiepNhan)
            .HasColumnName("ngaytiepnhan");

        builder.Property(x => x.NgayHoanThanhThucTe)
            .HasColumnName("ngayhoanthanhthucte");

        // TRUC A (§2.1) — mac dinh 3 do server dat (§10.4)
        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .HasDefaultValue(TrangThaiNv.ChuaTrienKhai)
            .IsRequired();

        // TRUC B (§2.2) — mac dinh null
        builder.Property(x => x.TrangThaiDvXuly)
            .HasColumnName("trangthaiDvXuly");

        // TRUC C (§2.3)
        builder.Property(x => x.TrangThaiXuLyGiaHan)
            .HasColumnName("trangthaixulygiahan");

        builder.Property(x => x.SoLanGiaHan)
            .HasColumnName("solangiahan")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.MucDoHt)
            .HasColumnName("mucdoht");

        builder.Property(x => x.PhanHoi)
            .HasColumnName("phanhoi")
            .HasMaxLength(GioiHan.DoDaiNoiDung);

        builder.Property(x => x.HsChatLuong)
            .HasColumnName("hsChatluong");

        builder.Property(x => x.UserIdGiaoViec)
            .HasColumnName("userIdGiaoViec")
            .IsRequired();

        builder.Property(x => x.UserIdCreate)
            .HasColumnName("useridcreate")
            .IsRequired();

        builder.Property(x => x.UnitCode)
            .HasColumnName("unitcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreateDate)
            .HasColumnName("createdate")
            .IsRequired();

        builder.Property(x => x.UpdateDate)
            .HasColumnName("updatedate")
            .IsRequired();

        // §4.2 — tro toi AI_GOIY_LOG. CO Y KHONG khai khoa ngoai:
        // AI_GOIY_LOG.idnvchitiet lai tro nguoc ve day => khai ca hai chieu se tao
        // vong phu thuoc khi chen du lieu. Giu dang cot thuong, rang buoc o tang nghiep vu.
        builder.Property(x => x.AiGoiYId)
            .HasColumnName("ai_goiy_id");

        // --- §8 T12: chi muc cho cac truong loc nong (§5.3 C3, §5.4 D5, §5.10 J1) ---
        builder.HasIndex(x => x.IdVb).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_idvb");
        builder.HasIndex(x => x.TrangThai).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_trangthai");
        builder.HasIndex(x => x.TrangThaiDvXuly).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_trangthaiDvXuly");
        builder.HasIndex(x => x.HanXuLyTh).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_hanxulyth");
        builder.HasIndex(x => x.UserIdGiaoViec).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_userIdGiaoViec");
        builder.HasIndex(x => x.LinhVuc).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_linhvuc");
        builder.HasIndex(x => x.UnitCode).HasDatabaseName("IX_DM_NHIEMVU_CHITIET_unitcode");

        // Quan he toi DM_VANBAN da khai o DmVanBanConfiguration.
        builder.HasMany(x => x.PhanCong)
            .WithOne(x => x.NhiemVu)
            .HasForeignKey(x => x.IdNvChiTiet)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.LichSuXuLy)
            .WithOne(x => x.NhiemVu)
            .HasForeignKey(x => x.IdCtnv)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.LichSuGiaHan)
            .WithOne(x => x.NhiemVu)
            .HasForeignKey(x => x.IdCtnv)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
