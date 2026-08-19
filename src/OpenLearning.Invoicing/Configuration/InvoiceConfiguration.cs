using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Invoicing.Models;

namespace OpenLearning.Invoicing.Configuration;

public class InvoiceRequestConfiguration : IEntityTypeConfiguration<InvoiceRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceRequest> builder)
    {
        builder.ToTable("InvoicingRequests");
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.TaxId).HasMaxLength(100);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.HasIndex(r => new { r.OrderId, r.Status });
        builder.HasIndex(r => r.Status);
        builder.HasOne(r => r.Student).WithMany().HasForeignKey(r => r.StudentUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.Property(i => i.Number).HasMaxLength(40).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(10, 2);
        builder.Property(i => i.VoidReason).HasMaxLength(500);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.OrderId);
    }
}
