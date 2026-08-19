using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.QuestionIO.Models;

namespace OpenLearning.QuestionIO.Configuration;

public class QuestionImportJobConfiguration : IEntityTypeConfiguration<QuestionImportJob>
{
    public void Configure(EntityTypeBuilder<QuestionImportJob> builder)
    {
        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FileKey).HasMaxLength(500).IsRequired();
        builder.Property(j => j.ErrorFileKey).HasMaxLength(500);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.AsyncIOJobId).IsUnique();
    }
}

public class QuestionRowErrorConfiguration : IEntityTypeConfiguration<QuestionRowError>
{
    public void Configure(EntityTypeBuilder<QuestionRowError> builder)
    {
        builder.Property(e => e.Field).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(e => e.JobId);
    }
}
