using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenLearning.Chat.Models;

namespace OpenLearning.Chat.Configuration;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.Property(m => m.Body).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.Type).HasMaxLength(20).IsRequired();
        builder.HasIndex(m => new { m.CourseId, m.SentAt });
        builder.HasIndex(m => new { m.LessonId, m.SentAt });
        builder.HasIndex(m => new { m.SessionId, m.SentAt });
        builder.HasOne(m => m.Course)
               .WithMany()
               .HasForeignKey(m => m.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
