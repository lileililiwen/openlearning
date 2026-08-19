using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Assessments.Models;

namespace OpenLearning.Assessments.Configuration;

public class IncorrectAnswerConfiguration : IEntityTypeConfiguration<IncorrectAnswer>
{
    public void Configure(EntityTypeBuilder<IncorrectAnswer> builder)
    {
        builder.Property(x => x.ChosenAnswer).HasMaxLength(2000);
        builder.Property(x => x.CorrectAnswer).HasMaxLength(2000);
        builder.HasIndex(x => new { x.UserId, x.ResolvedAt });
        builder.HasIndex(x => new { x.UserId, x.SourceType, x.SourceId });
        builder.HasOne(x => x.Question)
               .WithMany()
               .HasForeignKey(x => x.QuestionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookmarkedQuestionConfiguration : IEntityTypeConfiguration<BookmarkedQuestion>
{
    public void Configure(EntityTypeBuilder<BookmarkedQuestion> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.QuestionId }).IsUnique();
        builder.HasOne(x => x.Question)
               .WithMany()
               .HasForeignKey(x => x.QuestionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
