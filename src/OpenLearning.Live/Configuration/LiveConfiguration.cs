using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Live.Models;

namespace OpenLearning.Live.Configuration;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.StreamKey).HasMaxLength(100).IsRequired();
        builder.Property(s => s.StreamUrl).HasMaxLength(1000);
        builder.HasIndex(s => new { s.CourseId, s.StartsAt });
        builder.HasOne(s => s.Course).WithMany().HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Instructor).WithMany().HasForeignKey(s => s.InstructorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LiveCoHostConfiguration : IEntityTypeConfiguration<LiveCoHost>
{
    public void Configure(EntityTypeBuilder<LiveCoHost> builder)
    {
        builder.HasIndex(h => new { h.SessionId, h.UserId }).IsUnique();
        builder.HasOne(h => h.Session).WithMany(s => s.CoHosts).HasForeignKey(h => h.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LiveCheckInConfiguration : IEntityTypeConfiguration<LiveCheckIn>
{
    public void Configure(EntityTypeBuilder<LiveCheckIn> builder)
    {
        builder.HasIndex(c => new { c.SessionId, c.UserId }).IsUnique();
        builder.HasOne(c => c.Session).WithMany().HasForeignKey(c => c.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
