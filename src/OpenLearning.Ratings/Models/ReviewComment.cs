using OpenLearning.Auth.Models;

namespace OpenLearning.Ratings.Models;

/// <summary>A threaded follow-up comment under a review.</summary>
public class ReviewComment
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public Review? Review { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser? Author { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
