using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Configuration;

public class PhoneCodeConfiguration : IEntityTypeConfiguration<PhoneCode>
{
    public void Configure(EntityTypeBuilder<PhoneCode> builder)
    {
        builder.HasIndex(c => c.PhoneNumber);
        builder.Property(c => c.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(c => new { c.PhoneNumber, c.UsedAt });
    }
}
