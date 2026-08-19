namespace OpenLearning.Auth.Services;

/// <summary>
/// Single source of truth for whether a TeachingAssistant is assigned to a
/// class group. The concrete implementation lives in the class-groups module;
/// a null implementation ships here so TA surfaces are safe before it lands.
/// </summary>
public interface IClassAssignmentLookup
{
    Task<bool> IsAssignedAsync(string userId, int classGroupId);

    Task<IReadOnlyList<int>> ListAssignedClassIdsAsync(string userId);
}

/// <summary>Default implementation: no assignments exist.</summary>
public sealed class NullClassAssignmentLookup : IClassAssignmentLookup
{
    public Task<bool> IsAssignedAsync(string userId, int classGroupId)
    {
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<int>> ListAssignedClassIdsAsync(string userId)
    {
        return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }
}
