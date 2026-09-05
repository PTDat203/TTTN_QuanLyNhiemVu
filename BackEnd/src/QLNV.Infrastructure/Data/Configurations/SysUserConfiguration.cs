using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.7 — anh xa <see cref="SysUser"/> sang bang <c>SYS_USER</c>.
/// §10.11: cot <c>password_hash</c> chi luu chuoi bam BCrypt, khong bao gio luu mat khau ro.
/// </summary>
public sealed class SysUserConfiguration : IEntityTypeConfiguration<SysUser>
{
    public void Configure(EntityTypeBuilder<SysUser> builder)
    {
        builder.ToTable("SYS_USER");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserName)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnName("fullname")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(200);

        builder.Property(x => x.UnitCode)
            .HasColumnName("unitcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ChucVu)
            .HasColumnName("chucvu")
            .HasMaxLength(200);

        builder.Property(x => x.VaiTro)
            .HasColumnName("vaitro")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TrangThai)
            .HasColumnName("trangthai")
            .IsRequired();

        // MOI so voi goc — nguong tai K cho §9.4 S4
        builder.Property(x => x.MaxConcurrentTasks)
            .HasColumnName("max_concurrent_tasks")
            .IsRequired();

        builder.Property(x => x.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(500);

        builder.Property(x => x.RefreshTokenHetHan)
            .HasColumnName("refresh_token_hethan");

        builder.Property(x => x.CreateDate)
            .HasColumnName("createdate")
            .IsRequired();

        // Thuoc tinh TINH — khong phai cot CSDL
        builder.Ignore(x => x.DangHoatDong);

        builder.HasIndex(x => x.UserName)
            .IsUnique()
            .HasDatabaseName("UX_SYS_USER_username");
        builder.HasIndex(x => x.UnitCode).HasDatabaseName("IX_SYS_USER_unitcode");
        builder.HasIndex(x => x.VaiTro).HasDatabaseName("IX_SYS_USER_vaitro");

        builder.HasOne(x => x.DonVi)
            .WithMany(x => x.NguoiDung)
            .HasForeignKey(x => x.UnitCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
