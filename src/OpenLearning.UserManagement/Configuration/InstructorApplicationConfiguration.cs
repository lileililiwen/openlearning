using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.UserManagement.Models;

namespace OpenLearning.UserManagement.Configuration;

public class InstructorApplicationConfiguration : IEntityTypeConfiguration<InstructorApplication>
{
    public void Configure(EntityTypeBuilder<InstructorApplication> builder)
    {
        builder.HasIndex(a => a.UserId).IsUnique();
        builder.Property(a => a.Motivation).HasMaxLength(2000);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);
        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
