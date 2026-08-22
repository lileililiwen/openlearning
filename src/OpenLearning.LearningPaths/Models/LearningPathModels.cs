namespace OpenLearning.LearningPaths.Models;

public sealed class LearningPath
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<LearningPathVersion> Versions { get; set; } = new List<LearningPathVersion>();
}

public sealed class LearningPathVersion
{
    public int Id { get; set; }
    public int LearningPathId { get; set; }
    public LearningPath? LearningPath { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ICollection<LearningPathStage> Stages { get; set; } = new List<LearningPathStage>();
}

public sealed class LearningPathStage
{
    public int Id { get; set; }
    public int LearningPathVersionId { get; set; }
    public LearningPathVersion? Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public int MinimumElectives { get; set; }
    public ICollection<LearningPathCourse> Courses { get; set; } = new List<LearningPathCourse>();
}

public sealed class LearningPathCourse
{
    public int Id { get; set; }
    public int LearningPathStageId { get; set; }
    public LearningPathStage? Stage { get; set; }
    public int CourseId { get; set; }
    public bool IsRequired { get; set; }
    public int Position { get; set; }
    public int? PrerequisiteCourseId { get; set; }
}

public sealed class PathEnrollment
{
    public int Id { get; set; }
    public int LearningPathVersionId { get; set; }
    public LearningPathVersion? Version { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
