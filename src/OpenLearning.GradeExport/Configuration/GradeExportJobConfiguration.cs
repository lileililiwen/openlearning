using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.GradeExport.Models;

namespace OpenLearning.GradeExport.Configuration;

public class GradeExportJobConfiguration : IEntityTypeConfiguration<GradeExportJob>
{
    public void Configure(EntityTypeBuilder<GradeExportJob> builder)
    {
        builder.Property(j => j.UserId).HasMaxLength(450).IsRequired();
        builder.Property(j => j.FiltersJson).HasMaxLength(4000);
        builder.Property(j => j.FileKey).HasMaxLength(500);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}
