using OpenLearning.StudentIO.Models;

namespace OpenLearning.StudentIO.Services;

/// <summary>Restricts an import to a TA's assigned class scope.</summary>
public sealed record StudentImportScope(bool IsTa, int? RequiredClassGroupId = null);

/// <summary>One parsed data row from the Excel template.</summary>
internal sealed record StudentParsedRow(
    int RowIndex,
    string? ActionText,
    string? Email,
    string? Phone,
    string? DisplayName,
    string? Password,
    string? CourseIdsText,
    string? ClassGroupIdsText);

/// <summary>Validated values ready to execute.</summary>
internal sealed record StudentRowInput(
    StudentRowAction Action,
    string Email,
    string? Phone,
    string DisplayName,
    string? Password,
    List<int> CourseIds,
    List<int> ClassGroupIds);
