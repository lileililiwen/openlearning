using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Community.Models;

/// <summary>A community post (course-wide or class-scoped via ClassGroupId).</summary>
public class Post
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser? Author { get; set; }

    public string Body { get; set; } = string.Empty;

    public int? ClassGroupId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PostReply> Replies { get; set; } = new List<PostReply>();
}

public class PostReply
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public Post? Post { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser? Author { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
