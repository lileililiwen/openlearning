using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Services;

/// <summary>
/// Reviews real-name verification requests. Only the ApplicationUser fields
/// change here; notifying the applicant is composed in the admin page so this
/// module stays free of a Notifications dependency.
/// </summary>
public class IdentityService
{
    private readonly DbContext _db;

    public IdentityService(DbContext db)
    {
        _db = db;
    }

    public Task<List<ApplicationUser>> GetPendingAsync()
    {
        return _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.IdentityStatus == IdentityStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }

    public Task<List<ApplicationUser>> GetReviewedAsync(int count)
    {
        return _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.IdentityStatus == IdentityStatus.Verified ||
                        u.IdentityStatus == IdentityStatus.Rejected)
            .OrderByDescending(u => u.VerifiedAt)
            .ThenByDescending(u => u.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public Task<ApplicationUser?> GetByIdAsync(string userId)
    {
        return _db.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<(bool Ok, string? Error)> ApproveAsync(string userId, string? note)
    {
        var user = await GetByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        user.IdentityStatus = IdentityStatus.Verified;
        user.VerifiedAt = DateTime.UtcNow;
        user.VerificationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RejectAsync(string userId, string? note)
    {
        var user = await GetByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        user.IdentityStatus = IdentityStatus.Rejected;
        user.VerifiedAt = null;
        user.VerificationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
