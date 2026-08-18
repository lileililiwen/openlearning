using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).HasMaxLength(100);
        builder.Property(u => u.Bio).HasMaxLength(2000);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
    }
}
