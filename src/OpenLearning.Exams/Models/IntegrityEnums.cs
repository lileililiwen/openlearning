namespace OpenLearning.Exams.Models;

/// <summary>Allowlisted client integrity signals. No camera, microphone, or biometric capture.</summary>
public enum IntegrityEventType
{
    Heartbeat = 0,
    VisibilityHidden = 1,
    VisibilityVisible = 2,
    CopyAttempt = 3,
    PasteAttempt = 4,
    ConnectivityLost = 5,
    ConnectivityRestored = 6,
    TabSwitch = 7,
}

public enum IntegrityRiskLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

public enum IntegrityIncidentStatus
{
    Open = 0,
    UnderReview = 1,
    Dispositioned = 2,
    Appealed = 3,
    Closed = 4,
}

public enum IntegrityDispositionOutcome
{
    NoAction = 0,
    Warning = 1,
    ScoreAdjustment = 2,
    Invalidated = 3,
}

public enum IntegrityAppealStatus
{
    Submitted = 0,
    Upheld = 1,
    Overturned = 2,
}

public enum IntegritySessionStatus
{
    Active = 0,
    Closed = 1,
}

public enum IntegrityAccessAction
{
    ViewIncident = 0,
    ViewEvidence = 1,
    RecordDisposition = 2,
    DecideAppeal = 3,
}
