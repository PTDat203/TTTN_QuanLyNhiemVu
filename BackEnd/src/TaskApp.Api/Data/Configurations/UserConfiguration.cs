using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>
/// Anh xa <see cref="User"/> vao bang USERS.
/// Moi ten bang va ten cot deu map VIET HOA tuong minh: Oracle viet hoa dinh danh
/// khong dat trong nhay kep, con EF Core LUON dat nhay kep - khong map tuong minh
/// thi EF di tim bang "Users" va bao ORA-00942.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Cau hinh anh xa cho thuc the <see cref="User"/>.</summary>
    /// <param name="e">Bo dung cau hinh thuc the.</param>
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("USERS");

        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        e.Property(x => x.Username)
            .HasColumnName("USERNAME")
            .HasMaxLength(50)
            .IsRequired();

        e.Property(x => x.PasswordHash)
            .HasColumnName("PASSWORD_HASH")
            .HasMaxLength(255)
            .IsRequired();

        e.Property(x => x.FullName)
            .HasColumnName("FULL_NAME")
            .HasMaxLength(100)
            .IsRequired();

        e.Property(x => x.Email)
            .HasColumnName("EMAIL")
            .HasMaxLength(100);

        e.Property(x => x.Role)
            .HasColumnName("USER_ROLE")
            .HasMaxLength(20)
            .IsRequired();

        e.Property(x => x.Status)
            .HasColumnName("USER_STATUS")
            .HasMaxLength(20)
            .IsRequired();

        e.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT");

        e.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT");

        // Trung voi rang buoc UQ_USERS_USERNAME trong DDL
        e.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UQ_USERS_USERNAME");

        // UQ_USERS_EMAIL co that trong DB: hai nguoi dung khong duoc trung email.
        // EMAIL cho phep NULL, va Oracle cho phep nhieu dong NULL trong index UNIQUE.
        e.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("UQ_USERS_EMAIL");
    }
}
