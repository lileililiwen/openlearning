namespace OpenLearning.Logging.Models;

/// <summary>Audit record of a significant user or admin mutation.</summary>
public class OperationLog
{
    public int Id { get; set; }

    public string? ActorId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
