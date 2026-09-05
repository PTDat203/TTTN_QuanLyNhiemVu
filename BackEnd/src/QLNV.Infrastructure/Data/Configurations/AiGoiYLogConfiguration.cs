using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QLNV.Core.Entities;

namespace QLNV.Infrastructure.Data.Configurations;

/// <summary>
/// §4.8 — anh xa <see cref="AiGoiYLog"/> sang bang <c>AI_GOIY_LOG</c>.
///
/// CO Y KHONG khai khoa ngoai toi <c>DM_NHIEMVU_CHITIET</c>: <c>idnvchitiet</c> co the
/// null khi goi y cho dong nhiem vu DANG TAO MOI (chua co ma) — §3.2 M05/M06 — va
/// <c>DM_NHIEMVU_CHITIET.ai_goiy_id</c> lai tro nguoc lai day, khai ca hai chieu se
/// tao vong phu thuoc khi chen du lieu.
/// </summary>
public sealed class AiGoiYLogConfiguration : IEntityTypeConfiguration<AiGoiYLog>
{
    public void Configure(EntityTypeBuilder<AiGoiYLog> builder)
    {
        builder.ToTable("AI_GOIY_LOG");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.IdNvChiTiet)
            .HasColumnName("idnvchitiet");

        builder.Property(x => x.LinhVuc)
            .HasColumnName("linhvuc")
            .HasMaxLength(50);

        // nvarchar(max) / TEXT — KHONG dat HasMaxLength
        builder.Property(x => x.KetQuaJson)
            .HasColumnName("ket_qua_json");

        builder.Property(x => x.UserIdDaChon)
            .HasColumnName("userid_da_chon");

        builder.Property(x => x.ThuHangDaChon)
            .HasColumnName("thu_hang_da_chon");

        builder.Property(x => x.CoTrongGoiY)
            .HasColumnName("co_trong_goi_y")
            .IsRequired();

        // §4.8 khai kieu int; dung CHUOI de ghi duoc "v1.0"/"v1.1" nhu §9.6 tra ve
        builder.Property(x => x.PhienBanTrongSo)
            .HasColumnName("phien_ban_trongso")
            .HasMaxLength(20);

        builder.Property(x => x.CreateDate)
            .HasColumnName("createdate")
            .IsRequired();

        // --- BO SUNG ngoai §4.8 ---
        builder.Property(x => x.UserIdGoiY)
            .HasColumnName("userid_goiy");

        builder.Property(x => x.CheDo)
            .HasColumnName("che_do")
            .HasMaxLength(20);

        builder.Property(x => x.SoUngVien)
            .HasColumnName("so_ung_vien")
            .IsRequired();

        // §9.8 — thong ke Precision@k / MRR quet theo linh vuc va thoi diem
        builder.HasIndex(x => x.IdNvChiTiet).HasDatabaseName("IX_AI_GOIY_LOG_idnvchitiet");
        builder.HasIndex(x => x.CreateDate).HasDatabaseName("IX_AI_GOIY_LOG_createdate");
        builder.HasIndex(x => x.LinhVuc).HasDatabaseName("IX_AI_GOIY_LOG_linhvuc");
    }
}
