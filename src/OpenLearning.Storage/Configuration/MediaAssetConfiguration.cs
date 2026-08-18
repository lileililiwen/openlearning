using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Storage.Models;

namespace OpenLearning.Storage.Configuration;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasIndex(m => m.StoredFileId).IsUnique();
        builder.Property(m => m.LowUrl).HasMaxLength(400);
        builder.Property(m => m.MidUrl).HasMaxLength(400);
        builder.Property(m => m.HighUrl).HasMaxLength(400);
        builder.Property(m => m.Error).HasMaxLength(500);
        builder.HasOne(m => m.StoredFile)
               .WithMany()
               .HasForeignKey(m => m.StoredFileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
