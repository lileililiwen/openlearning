using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.LearningPaths.Models;

namespace OpenLearning.LearningPaths.Configuration;

public sealed class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => new { x.OwnerId, x.IsArchived });
    }
}

public sealed class LearningPathVersionConfiguration : IEntityTypeConfiguration<LearningPathVersion>
{
    public void Configure(EntityTypeBuilder<LearningPathVersion> builder)
    {
        builder.HasIndex(x => new { x.LearningPathId, x.VersionNumber }).IsUnique();
        builder.HasOne(x => x.LearningPath).WithMany(x => x.Versions).HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LearningPathStageConfiguration : IEntityTypeConfiguration<LearningPathStage>
{
    public void Configure(EntityTypeBuilder<LearningPathStage> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.LearningPathVersionId, x.Position }).IsUnique();
        builder.HasOne(x => x.Version).WithMany(x => x.Stages).HasForeignKey(x => x.LearningPathVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LearningPathCourseConfiguration : IEntityTypeConfiguration<LearningPathCourse>
{
    public void Configure(EntityTypeBuilder<LearningPathCourse> builder)
    {
        builder.HasIndex(x => new { x.LearningPathStageId, x.CourseId }).IsUnique();
        builder.HasIndex(x => x.CourseId);
        builder.HasOne(x => x.Stage).WithMany(x => x.Courses).HasForeignKey(x => x.LearningPathStageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PathEnrollmentConfiguration : IEntityTypeConfiguration<PathEnrollment>
{
    public void Configure(EntityTypeBuilder<PathEnrollment> builder)
    {
        builder.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => new { x.StudentId, x.LearningPathVersionId }).IsUnique();
        builder.HasOne(x => x.Version).WithMany().HasForeignKey(x => x.LearningPathVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
