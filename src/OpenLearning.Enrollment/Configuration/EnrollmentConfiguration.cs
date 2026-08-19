using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Enrollment.Configuration;

public class EnrollmentConfiguration : IEntityTypeConfiguration<EnrollmentEntity>
{
    public void Configure(EntityTypeBuilder<EnrollmentEntity> builder)
    {
        // Not unique: a revoked/expired enrollment is re-enrollable, so the
        // same (student, course) pair may legitimately have multiple rows.
        builder.HasIndex(e => new { e.StudentId, e.CourseId });
        builder.HasIndex(e => e.ClassGroupId);
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Course)
               .WithMany()
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
