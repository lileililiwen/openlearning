using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Operations.Models;

namespace OpenLearning.Operations.Configuration;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(b => b.LinkUrl).HasMaxLength(1000).IsRequired();
        builder.HasOne(b => b.Campaign)
            .WithMany(c => c.Banners)
            .HasForeignKey(b => b.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PopupConfiguration : IEntityTypeConfiguration<Popup>
{
    public void Configure(EntityTypeBuilder<Popup> builder)
    {
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Body).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.LinkUrl).HasMaxLength(1000).IsRequired();
    }
}

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
    }
}

public class HomepageFeatureConfiguration : IEntityTypeConfiguration<HomepageFeature>
{
    public void Configure(EntityTypeBuilder<HomepageFeature> builder)
    {
        builder.Property(f => f.Category).HasMaxLength(100);
    }
}
