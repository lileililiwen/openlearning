namespace OpenLearning.CourseManagement.Models;

/// <summary>A flat, de-duplicated tag from the shared vocabulary.</summary>
public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe lowercase key used for catalog filters.</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
}

/// <summary>Join between a course and a tag (composite key).</summary>
public class CourseTag
{
    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public int TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
