using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Configuration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000);
        builder.Property(n => n.Link).HasMaxLength(500);
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
        builder.HasIndex(n => n.IsRead);
    }
}

public class CourseAnnouncementConfiguration : IEntityTypeConfiguration<CourseAnnouncement>
{
    public void Configure(EntityTypeBuilder<CourseAnnouncement> builder)
    {
        builder.Property(a => a.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(a => new { a.CourseId, a.CreatedAt });
        builder.HasOne(a => a.Course)
               .WithMany()
               .HasForeignKey(a => a.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
