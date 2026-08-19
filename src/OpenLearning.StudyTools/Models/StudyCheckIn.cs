namespace OpenLearning.StudyTools.Models;

/// <summary>One daily study check-in; a second check-in the same day upserts.</summary>
public class StudyCheckIn
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateOnly Day { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
