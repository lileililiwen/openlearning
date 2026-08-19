using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.StudyTools.Models;

namespace OpenLearning.StudyTools.Configuration;

public class LessonNoteConfiguration : IEntityTypeConfiguration<LessonNote>
{
    public void Configure(EntityTypeBuilder<LessonNote> builder)
    {
        builder.Property(n => n.Body).HasMaxLength(20000).IsRequired();
        builder.HasIndex(n => new { n.UserId, n.LessonId }).IsUnique();
        builder.HasIndex(n => n.LessonId);
        builder.HasOne(n => n.Lesson)
               .WithMany()
               .HasForeignKey(n => n.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StudyCheckInConfiguration : IEntityTypeConfiguration<StudyCheckIn>
{
    public void Configure(EntityTypeBuilder<StudyCheckIn> builder)
    {
        builder.Property(c => c.Note).HasMaxLength(1000);
        builder.HasIndex(c => new { c.UserId, c.Day }).IsUnique();
    }
}

public class LessonDownloadConfiguration : IEntityTypeConfiguration<LessonDownload>
{
    public void Configure(EntityTypeBuilder<LessonDownload> builder)
    {
        builder.Property(d => d.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Label).HasMaxLength(200).IsRequired();
        builder.HasIndex(d => d.LessonId);
        builder.HasOne(d => d.Lesson)
               .WithMany()
               .HasForeignKey(d => d.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StudyDailyAggregateConfiguration : IEntityTypeConfiguration<StudyDailyAggregate>
{
    public void Configure(EntityTypeBuilder<StudyDailyAggregate> builder)
    {
        builder.HasIndex(a => new { a.Date, a.UserId, a.CourseId }).IsUnique();
    }
}
