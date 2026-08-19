namespace OpenLearning.StudyTools.Models;

/// <summary>Per-day, per-student, per-course study summary produced by the daily aggregate job.</summary>
public class StudyDailyAggregate
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public int TotalSeconds { get; set; }

    public int LessonsCompleted { get; set; }
}
