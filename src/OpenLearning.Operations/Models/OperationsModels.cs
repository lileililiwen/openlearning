namespace OpenLearning.Operations.Models;

/// <summary>A homepage carousel slide.</summary>
public class Banner
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string LinkUrl { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CampaignId { get; set; }

    public Campaign? Campaign { get; set; }
}

/// <summary>A scheduled announcement pop-up shown once per session.</summary>
public class Popup
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string LinkUrl { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>A named promotion that groups banners within a date window.</summary>
public class Campaign
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Banner> Banners { get; set; } = new List<Banner>();
}

/// <summary>Admin-picked featured category or course shown on the homepage.</summary>
public class HomepageFeature
{
    public int Id { get; set; }

    /// <summary>Category name when the feature is a category; null for course features.</summary>
    public string? Category { get; set; }

    /// <summary>Course id when the feature is a course; null for category features.</summary>
    public int? CourseId { get; set; }

    public int OrderIndex { get; set; }
}
