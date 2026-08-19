using System.Text.Json;

namespace OpenLearning.GradeExport.Services;

/// <summary>
/// Filters shared by every export kind. Only the fields relevant to a kind are
/// populated by its page; serialized into <c>GradeExportJob.FiltersJson</c> so
/// the async processor (and the audit trail) can replay the same export.
/// </summary>
public sealed record GradeExportFilters(
    int? CourseId,
    int? AssignmentId,
    int? QuizId,
    int? ExamId,
    int? ClassGroupId,
    DateTime? From,
    DateTime? To,
    bool? GradedOnly,
    bool IsTaScope,
    bool IsAdmin)
{
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static GradeExportFilters? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GradeExportFilters>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
