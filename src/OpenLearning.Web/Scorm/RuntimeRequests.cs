namespace OpenLearning.Web.Scorm;

/// <summary>Request body for the SCORM 1.2 runtime API (initialize).</summary>
public sealed record ScormInitRequest(int PackageId);

/// <summary>Request body for the SCORM 1.2 runtime API (commit).</summary>
public sealed record ScormCommitRequest(
    int PackageId,
    string? LessonLocation,
    string? SuspendData,
    string? LessonStatus,
    string? ScoreRaw,
    string? SessionTime);
