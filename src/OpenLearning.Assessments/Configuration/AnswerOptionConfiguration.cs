using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.Property(o => o.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(o => new { o.QuestionId, o.OrderIndex });
        builder.HasOne(o => o.Question)
               .WithMany(q => q.AnswerOptions)
               .HasForeignKey(o => o.QuestionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
