namespace OpenLearning.Logging.Models;

/// <summary>Persisted record of an unhandled exception with request context.</summary>
public class ErrorLog
{
    public int Id { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? Path { get; set; }

    public string? RequestMethod { get; set; }

    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
