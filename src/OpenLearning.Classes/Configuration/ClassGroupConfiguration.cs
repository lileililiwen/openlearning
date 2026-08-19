using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Classes.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using NotificationEntity = OpenLearning.Notifications.Models.Notification;

namespace OpenLearning.Classes.Configuration;

public class ClassGroupConfiguration : IEntityTypeConfiguration<ClassGroup>
{
    public void Configure(EntityTypeBuilder<ClassGroup> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => new { c.CourseId, c.StartsAt });
        builder.HasOne(c => c.Course)
               .WithMany()
               .HasForeignKey(c => c.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        // Enrollment.ClassGroupId is a scalar on the Enrollment entity (owned by
        // OpenLearning.Enrollment); the relationship is configured here so the
        // Enrollment module never needs to reference this module.
        builder.HasMany<EnrollmentEntity>()
               .WithOne()
               .HasForeignKey(e => e.ClassGroupId)
               .OnDelete(DeleteBehavior.SetNull);

        // Notification.ClassGroupId is a scalar on the Notification entity (owned by
        // OpenLearning.Notifications); the relationship is configured here so the
        // Notifications module never needs to reference this module.
        builder.HasMany<NotificationEntity>()
               .WithOne()
               .HasForeignKey(n => n.ClassGroupId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ClassAssignmentConfiguration : IEntityTypeConfiguration<ClassAssignment>
{
    public void Configure(EntityTypeBuilder<ClassAssignment> builder)
    {
        builder.HasIndex(a => new { a.ClassGroupId, a.UserId, a.Role }).IsUnique();
        builder.HasIndex(a => new { a.UserId, a.ClassGroupId });
        builder.HasOne(a => a.ClassGroup)
               .WithMany()
               .HasForeignKey(a => a.ClassGroupId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
