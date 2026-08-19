namespace OpenLearning.Moderation.Models;

public enum ReportedContentType
{
    Review = 0,
    ReviewComment = 1,
    Question = 2,
    QuestionReply = 3,
    Post = 4,
    PostReply = 5,
}

public enum ReportResolution
{
    Pending = 0,
    Removed = 1,
    Dismissed = 2,
}

/// <summary>A user report against a review, comment, or community post/question.</summary>
public class ContentReport
{
    public int Id { get; set; }

    public ReportedContentType ContentType { get; set; }

    public int ContentId { get; set; }

    public string ReportedById { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public ReportResolution Resolution { get; set; } = ReportResolution.Pending;

    public string? ResolvedById { get; set; }
}
