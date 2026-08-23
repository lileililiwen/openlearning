using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Credits.Models;

namespace OpenLearning.Credits.Configuration;

public class CreditAwardConfiguration : IEntityTypeConfiguration<CreditAward>
{
    public void Configure(EntityTypeBuilder<CreditAward> builder)
    {
        builder.Property(a => a.Amount).HasPrecision(10, 2);
        builder.Property(a => a.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SourceId).HasMaxLength(200);
        builder.Property(a => a.Reason).HasMaxLength(1000);
        builder.HasIndex(a => new { a.StudentId, a.SourceType, a.SourceId }).IsUnique();
        builder.HasIndex(a => a.StudentId);
        builder.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GraduationProgramConfiguration : IEntityTypeConfiguration<GraduationProgram>
{
    public void Configure(EntityTypeBuilder<GraduationProgram> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CategoryMinimums).HasMaxLength(2000);
        builder.Property(p => p.RequiredCourseIds).HasMaxLength(2000);
        builder.HasIndex(p => new { p.Name, p.Version }).IsUnique();
    }
}

public class CourseCreditRuleConfiguration : IEntityTypeConfiguration<CourseCreditRule>
{
    public void Configure(EntityTypeBuilder<CourseCreditRule> builder)
    {
        builder.Property(r => r.Amount).HasPrecision(10, 2);
        builder.HasIndex(r => new { r.CourseId, r.Version }).IsUnique();
        builder.HasIndex(r => new { r.CourseId, r.IsActive });
    }
}

public class LearnerProgramConfiguration : IEntityTypeConfiguration<LearnerProgram>
{
    public void Configure(EntityTypeBuilder<LearnerProgram> builder)
    {
        builder.HasIndex(lp => lp.StudentId).IsUnique();
        builder.HasOne(lp => lp.Student).WithMany().HasForeignKey(lp => lp.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(lp => lp.Program).WithMany().HasForeignKey(lp => lp.ProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GraduationDecisionConfiguration : IEntityTypeConfiguration<GraduationDecision>
{
    public void Configure(EntityTypeBuilder<GraduationDecision> builder)
    {
        builder.Property(d => d.Notes).HasMaxLength(2000);
        builder.HasIndex(d => new { d.StudentId, d.ProgramId });
        builder.HasOne(d => d.Student).WithMany().HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.Program).WithMany().HasForeignKey(d => d.ProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}
