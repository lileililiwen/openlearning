namespace OpenLearning.CourseManagement.Models;

/// <summary>An admin-managed course category (flat, ordered vocabulary).</summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe lowercase key used for catalog filters.</summary>
    public string Slug { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;
}
