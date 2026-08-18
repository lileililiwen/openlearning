using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Certificates.Models;

namespace OpenLearning.Certificates.Configuration;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.EnrollmentId).IsUnique();
        builder.HasOne(c => c.Course)
               .WithMany()
               .HasForeignKey(c => c.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
