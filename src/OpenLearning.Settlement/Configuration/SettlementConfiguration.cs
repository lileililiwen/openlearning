using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Settlement.Models;

namespace OpenLearning.Settlement.Configuration;

public class SettlementLedgerConfiguration : IEntityTypeConfiguration<SettlementLedger>
{
    public void Configure(EntityTypeBuilder<SettlementLedger> builder)
    {
        builder.Property(l => l.Amount).HasPrecision(10, 2);
        builder.Property(l => l.Reason).HasMaxLength(200).IsRequired();
        builder.HasIndex(l => new { l.InstructorId, l.CreatedAt });
        builder.HasOne(l => l.Course)
               .WithMany()
               .HasForeignKey(l => l.CourseId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.Property(w => w.Amount).HasPrecision(10, 2);
        builder.HasIndex(w => new { w.InstructorId, w.Status });
        builder.HasIndex(w => w.Status);
    }
}
