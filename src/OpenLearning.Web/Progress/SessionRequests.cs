namespace OpenLearning.Web.Progress;

/// <summary>Request body for the study-session start endpoint.</summary>
public sealed record SessionStartRequest(int CourseId, int LessonId);

/// <summary>Request body for the study-session heartbeat and end endpoints.</summary>
public sealed record SessionHeartbeatRequest(int SessionId);

/// <summary>Request body for the video playback position save endpoint.</summary>
public sealed record PositionSaveRequest(int CourseId, int LessonId, int Seconds);
