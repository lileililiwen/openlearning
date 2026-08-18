using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.Property(q => q.Title).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Description).HasMaxLength(2000);
        builder.HasIndex(q => q.CourseId);
        builder.HasOne(q => q.Course)
               .WithMany()
               .HasForeignKey(q => q.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
