using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Community.Models;

namespace OpenLearning.Community.Configuration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.Title).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(q => new { q.CourseId, q.CreatedAt });
        builder.HasIndex(q => new { q.CourseId, q.ClassGroupId });
        builder.HasOne(q => q.Course).WithMany().HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(q => q.Author).WithMany().HasForeignKey(q => q.AuthorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuestionReplyConfiguration : IEntityTypeConfiguration<QuestionReply>
{
    public void Configure(EntityTypeBuilder<QuestionReply> builder)
    {
        builder.Property(r => r.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(r => new { r.QuestionId, r.AuthorId, r.Body }).IsUnique();
        builder.HasOne(r => r.Question).WithMany(q => q.Replies).HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Author).WithMany().HasForeignKey(r => r.AuthorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(p => new { p.CourseId, p.CreatedAt });
        builder.HasIndex(p => new { p.CourseId, p.ClassGroupId });
        builder.HasOne(p => p.Course).WithMany().HasForeignKey(p => p.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostReplyConfiguration : IEntityTypeConfiguration<PostReply>
{
    public void Configure(EntityTypeBuilder<PostReply> builder)
    {
        builder.Property(r => r.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(r => new { r.PostId, r.AuthorId, r.Body }).IsUnique();
        builder.HasOne(r => r.Post).WithMany(p => p.Replies).HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Author).WithMany().HasForeignKey(r => r.AuthorId).OnDelete(DeleteBehavior.Cascade);
    }
}
