using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assignments.Models;

namespace OpenLearning.Assignments.Configuration;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Instructions).HasMaxLength(4000).IsRequired();
        builder.HasIndex(a => a.CourseId);
        builder.HasIndex(a => a.AuthorId);
        builder.HasMany(a => a.Submissions)
            .WithOne(s => s.Assignment)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.Property(s => s.Text).HasMaxLength(8000);
        builder.Property(s => s.FileUrl).HasMaxLength(1000);
        builder.Property(s => s.Feedback).HasMaxLength(2000);
        builder.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
        builder.HasIndex(s => s.StudentId);
    }
}
