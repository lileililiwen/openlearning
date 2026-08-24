using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Mobile.Models;

namespace OpenLearning.Mobile.Configuration;

public class DeviceSessionConfiguration : IEntityTypeConfiguration<DeviceSession>
{
    public void Configure(EntityTypeBuilder<DeviceSession> builder)
    {
        builder.Property(s => s.UserId).HasMaxLength(450).IsRequired();
        builder.Property(s => s.DeviceId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DeviceName).HasMaxLength(200);
        builder.Property(s => s.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(s => s.TokenFamilyId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.RevokedReason).HasMaxLength(50);
        builder.HasIndex(s => new { s.UserId, s.DeviceId });
        builder.HasIndex(s => s.TokenFamilyId).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(t => t.UserId).HasMaxLength(450).IsRequired();
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.FamilyId).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.FamilyId, t.Rotation });
    }
}

public class OfflineManifestConfiguration : IEntityTypeConfiguration<OfflineManifest>
{
    public void Configure(EntityTypeBuilder<OfflineManifest> builder)
    {
        builder.Property(m => m.UserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(m => new { m.UserId, m.CourseId });
    }
}

public class OfflineManifestAssetConfiguration : IEntityTypeConfiguration<OfflineManifestAsset>
{
    public void Configure(EntityTypeBuilder<OfflineManifestAsset> builder)
    {
        builder.Property(a => a.StorageKey).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.FileName).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Checksum).HasMaxLength(128).IsRequired();
        builder.HasOne(a => a.Manifest)
               .WithMany(m => m.Assets)
               .HasForeignKey(a => a.ManifestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SyncOperationConfiguration : IEntityTypeConfiguration<SyncOperation>
{
    public void Configure(EntityTypeBuilder<SyncOperation> builder)
    {
        builder.Property(o => o.UserId).HasMaxLength(450).IsRequired();
        builder.Property(o => o.OperationId).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CanonicalState).HasMaxLength(4000);
        builder.HasIndex(o => new { o.UserId, o.OperationId }).IsUnique();
    }
}

public class MobilePushDeviceConfiguration : IEntityTypeConfiguration<MobilePushDevice>
{
    public void Configure(EntityTypeBuilder<MobilePushDevice> builder)
    {
        builder.Property(d => d.UserId).HasMaxLength(450).IsRequired();
        builder.Property(d => d.DeviceId).HasMaxLength(200).IsRequired();
        builder.Property(d => d.PushToken).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Provider).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => new { d.UserId, d.DeviceId }).IsUnique();
    }
}
