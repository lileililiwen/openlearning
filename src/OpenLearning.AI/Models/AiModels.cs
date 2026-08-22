namespace OpenLearning.AI.Models;

public enum AiFeature { CourseQuestion = 0, DraftFeedback = 1, GradeSuggestion = 2 }
public enum AiAuditOutcome { Succeeded = 0, Disabled = 1, InsufficientSources = 2, QuotaExceeded = 3, Rejected = 4, ProviderFailed = 5 }

public sealed class AiPolicy
{
    public int Id { get; set; }
    public int? CourseId { get; set; }
    public string Provider { get; set; } = "sandbox";
    public string Model { get; set; } = "deterministic-v1";
    public string SecretReference { get; set; } = string.Empty;
    public bool QuestionsEnabled { get; set; }
    public bool DraftFeedbackEnabled { get; set; }
    public bool GradeSuggestionsEnabled { get; set; }
    public int DailyRequestQuota { get; set; } = 10;
    public int RetentionDays { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 15;
    public decimal CostPerThousandTokens { get; set; }
    public string ExternalProcessingDisclosure { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AiApprovedSource
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Anchor { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsApproved { get; set; }
    public bool IsUnsafe { get; set; }
    public string ApprovedById { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAt { get; set; }
}

public sealed class AiConversation
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

public sealed class AiMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public bool IsGenerated { get; set; } = true;
    public bool IsUncertain { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AiCitation> Citations { get; set; } = new List<AiCitation>();
}

public sealed class AiCitation
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public AiMessage? Message { get; set; }
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Anchor { get; set; } = string.Empty;
}

public sealed class AiFeedbackDraft
{
    public int Id { get; set; }
    public int AssignmentSubmissionId { get; set; }
    public string RequestedById { get; set; } = string.Empty;
    public int? SuggestedScore { get; set; }
    public string SuggestedFeedback { get; set; } = string.Empty;
    public string RubricEvidence { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public string? ConfirmedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}

public sealed class AiUsageAudit
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? CourseId { get; set; }
    public AiFeature Feature { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public AiAuditOutcome Outcome { get; set; }
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AiOutputReport
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public string ReportedById { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
