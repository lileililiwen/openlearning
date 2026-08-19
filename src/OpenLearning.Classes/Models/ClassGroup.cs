using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Classes.Models;

public enum ClassGroupStatus
{
    Upcoming = 0,
    Open = 1,
    Closed = 2,
}

public enum ClassAssignmentRole
{
    Instructor = 0,
    TeachingAssistant = 1,
    Observer = 2,
}

/// <summary>A term / cohort under a course (e.g. "2026 Spring", "VIP fast-track").</summary>
public class ClassGroup
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    /// <summary>Optional enrollment cap; null means unlimited.</summary>
    public int? Capacity { get; set; }

    /// <summary>Stored status: Upcoming or Closed. Open is derived from the time window.</summary>
    public ClassGroupStatus Status { get; set; } = ClassGroupStatus.Upcoming;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Open while StartsAt ≤ now ≤ EndsAt; Closed once EndsAt passes.</summary>
    public ClassGroupStatus EffectiveStatus
    {
        get
        {
            var now = DateTime.UtcNow;
            if (EndsAt < now)
            {
                return ClassGroupStatus.Closed;
            }

            if (StartsAt <= now)
            {
                return ClassGroupStatus.Open;
            }

            return Status;
        }
    }
}

/// <summary>Assignment of a user (TA / Instructor / Observer) to a class group.</summary>
public class ClassAssignment
{
    public int Id { get; set; }

    public int ClassGroupId { get; set; }

    public ClassGroup? ClassGroup { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ClassAssignmentRole Role { get; set; } = ClassAssignmentRole.TeachingAssistant;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
