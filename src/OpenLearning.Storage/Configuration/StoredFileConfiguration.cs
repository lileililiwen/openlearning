using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Storage.Models;

namespace OpenLearning.Storage.Configuration;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasIndex(f => f.Key).IsUnique();
        builder.Property(f => f.Key).HasMaxLength(255).IsRequired();
        builder.Property(f => f.OriginalName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(f => f.OwnerId).HasMaxLength(450).IsRequired();
    }
}
