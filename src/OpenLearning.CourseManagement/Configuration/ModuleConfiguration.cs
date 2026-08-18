using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Configuration;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(m => new { m.CourseId, m.OrderIndex });
        builder.HasOne(m => m.Course)
               .WithMany(c => c.Modules)
               .HasForeignKey(m => m.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
