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

    // ===== Booking (optional) =====

    /// <summary>When false, existing enrolled-student live access is unchanged.</summary>
    public bool IsBookingEnabled { get; set; }

    public DateTime? BookingOpensAt { get; set; }

    public DateTime? BookingClosesAt { get; set; }

    /// <summary>0 = unlimited.</summary>
    public int Capacity { get; set; }

    /// <summary>After this time, confirmed bookings cannot be cancelled.</summary>
    public DateTime? CancellationDeadline { get; set; }

    public ICollection<LiveCoHost> CoHosts { get; set; } = new List<LiveCoHost>();

    public ICollection<LiveBooking> Bookings { get; set; } = new List<LiveBooking>();

    public ICollection<LiveWaitlist> Waitlist { get; set; } = new List<LiveWaitlist>();
}

public enum LiveBookingStatus
{
    Confirmed = 0,
    Cancelled = 1,
}

/// <summary>One learner's seat reservation for a live session.</summary>
public class LiveBooking
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public LiveSession? Session { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public LiveBookingStatus Status { get; set; } = LiveBookingStatus.Confirmed;

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAt { get; set; }
}

/// <summary>Ordered entry for a learner waiting for a seat.</summary>
public class LiveWaitlist
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public LiveSession? Session { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    /// <summary>FIFO position (1 = first in line).</summary>
    public int Position { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When promoted to a booking, or null if still waiting.</summary>
    public DateTime? PromotedAt { get; set; }
}

/// <summary>
/// Revocable token for a personal iCalendar feed. Stored as a hash;
/// the raw token is only shown once at creation.
/// </summary>
public class LiveCalendarToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>SHA-256 hash of the raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }
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
