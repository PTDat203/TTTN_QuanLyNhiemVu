using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Api.Entities;

namespace TaskApp.Api.Data.Configurations;

/// <summary>Ánh xạ bảng USER_QUALIFICATIONS.</summary>
public class UserQualificationConfiguration : IEntityTypeConfiguration<UserQualification>
{
    public void Configure(EntityTypeBuilder<UserQualification> e)
    {
        e.ToTable("USER_QUALIFICATIONS");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        e.Property(x => x.UserId).HasColumnName("USER_ID").IsRequired();
        e.Property(x => x.DegreeName).HasColumnName("DEGREE_NAME").HasMaxLength(150).IsRequired();
        e.Property(x => x.Major).HasColumnName("MAJOR").HasMaxLength(150).IsRequired();
        e.Property(x => x.DegreeLevel).HasColumnName("DEGREE_LEVEL").HasMaxLength(30);
        e.Property(x => x.School).HasColumnName("SCHOOL").HasMaxLength(200);
        e.Property(x => x.GradYear).HasColumnName("GRAD_YEAR");
        e.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
        e.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");

        e.HasOne(x => x.User)
            .WithMany(u => u.BangCap)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_QUALIFICATIONS_USER")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.UserId).HasDatabaseName("IDX_QUALIFICATIONS_USER_ID");
    }
}
