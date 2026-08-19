using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;

namespace OpenLearning.Classes.Services;

/// <summary>
/// Real implementation of <see cref="IClassAssignmentLookup"/> backed by
/// <see cref="ClassAssignment"/> rows. Registered by AddClassesModule, it
/// overrides the Auth default (NullClassAssignmentLookup) in the composition
/// root because it is registered later.
/// </summary>
public class ClassAssignmentLookup : IClassAssignmentLookup
{
    private readonly DbContext _db;

    public ClassAssignmentLookup(DbContext db)
    {
        _db = db;
    }

    public Task<bool> IsAssignedAsync(string userId, int classGroupId)
    {
        return _db.Set<ClassAssignment>()
            .AnyAsync(a => a.UserId == userId && a.ClassGroupId == classGroupId);
    }

    public async Task<IReadOnlyList<int>> ListAssignedClassIdsAsync(string userId)
    {
        return await _db.Set<ClassAssignment>()
            .Where(a => a.UserId == userId)
            .Select(a => a.ClassGroupId)
            .Distinct()
            .ToListAsync();
    }
}
