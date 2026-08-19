using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Moderation.Models;

namespace OpenLearning.Moderation.Configuration;

public class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(r => new { r.ContentType, r.ContentId, r.Resolution });
        builder.HasIndex(r => r.Resolution);
    }
}
