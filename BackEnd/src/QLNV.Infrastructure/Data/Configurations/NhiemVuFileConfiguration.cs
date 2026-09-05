using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.6 — anh xa <see cref="NhiemVuFile"/> sang bang <c>NHIEMVU_FILE</c>.
/// <c>recordid</c> co the null trong khoang tu luc tai len (§5.7 G1) den luc gan
/// vao ban ghi nghiep vu, nen KHONG khai khoa ngoai.
/// </summary>
public sealed class NhiemVuFileConfiguration : IEntityTypeConfiguration<NhiemVuFile>
{
    public void Configure(EntityTypeBuilder<NhiemVuFile> builder)
    {
        builder.ToTable("NHIEMVU_FILE");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.RecordId)
            .HasColumnName("recordid");

        builder.Property(x => x.LoaiBanGhi)
            .HasColumnName("loai_ban_ghi")
            .HasMaxLength(20);

        builder.Property(x => x.FileName)
            .HasColumnName("fileName")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FilePath)
            .HasColumnName("filePath")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .HasColumnName("fileSize")
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("contentType")
            .HasMaxLength(200);

        builder.Property(x => x.CreateBy)
            .HasColumnName("createBy")
            .IsRequired();

        builder.Property(x => x.CreateDate)
            .HasColumnName("createDate")
            .IsRequired();

        builder.HasIndex(x => new { x.RecordId, x.LoaiBanGhi })
            .HasDatabaseName("IX_NHIEMVU_FILE_recordid_loai_ban_ghi");
        builder.HasIndex(x => x.CreateBy).HasDatabaseName("IX_NHIEMVU_FILE_createBy");
    }
}
