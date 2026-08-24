namespace OpenLearning.Analytics.Models;

/// <summary>
/// Audit record for an analytics export: who requested it, the scope, the
/// filters applied, and when it happened.
/// </summary>
public class ExportAudit
{
    public long Id { get; set; }

    /// <summary>Id of the user who requested the export.</summary>
    public string RequesterId { get; set; } = string.Empty;

    /// <summary>Scope of the export, e.g. "admin" or "instructor".</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Filters applied to the export, serialized as JSON.</summary>
    public string FiltersJson { get; set; } = string.Empty;

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
