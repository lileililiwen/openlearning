using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Live.Models;

public enum LiveSessionStatus
{
    Scheduled = 0,
    Live = 1,
    Ended = 2,
}

/// <summary>A scheduled live streaming session on a course.</summary>
public class LiveSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public string InstructorId { get; set; } = string.Empty;

    public ApplicationUser? Instructor { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    /// <summary>Push/stream key. Secret — shown only to the instructor and co-hosts.</summary>
    public string StreamKey { get; set; } = string.Empty;

    /// <summary>HLS pull URL for the live stream, or empty until the provider is configured.</summary>
    public string StreamUrl { get; set; } = string.Empty;

    public LiveSessionStatus Status { get; set; } = LiveSessionStatus.Scheduled;

    /// <summary>StoredFile id of the recording attached when the session ends (replay).</summary>
    public int? RecordingFileId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LiveCoHost> CoHosts { get; set; } = new List<LiveCoHost>();
}

public class LiveCoHost
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public LiveSession? Session { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}

public class LiveCheckIn
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public LiveSession? Session { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;
}
