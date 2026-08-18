using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.UserManagement.Models;

namespace OpenLearning.UserManagement.Services;

public class InstructorApplicationService
{
    private readonly DbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorApplicationService(DbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Stores a pending application. One application per user: submitting again
    /// replaces the previous one (resetting its review state).
    /// </summary>
    public async Task<(bool Ok, string? Error)> SubmitAsync(string userId, string motivation)
    {
        var existing = await _db.Set<InstructorApplication>()
            .FirstOrDefaultAsync(a => a.UserId == userId);
        if (existing is not null)
        {
            existing.Motivation = motivation;
            existing.Status = InstructorApplicationStatus.Pending;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.ReviewedAt = null;
            existing.ReviewedBy = null;
            existing.RejectionReason = null;
        }
        else
        {
            _db.Set<InstructorApplication>().Add(new InstructorApplication
            {
                UserId = userId,
                Motivation = motivation,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<InstructorApplication?> GetForUserAsync(string userId)
        => _db.Set<InstructorApplication>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);

    public Task<List<InstructorApplication>> GetPendingAsync()
        => _db.Set<InstructorApplication>().AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.Status == InstructorApplicationStatus.Pending)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

    public Task<List<InstructorApplication>> GetReviewedAsync(int count)
        => _db.Set<InstructorApplication>().AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.Status != InstructorApplicationStatus.Pending)
            .OrderByDescending(a => a.ReviewedAt)
            .Take(count)
            .ToListAsync();

    public async Task<(bool Ok, string? Error)> ApproveAsync(int applicationId, string reviewerId)
    {
        var application = await _db.Set<InstructorApplication>()
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Status == InstructorApplicationStatus.Pending);
        if (application is null)
        {
            return (false, "Pending application not found.");
        }

        var user = await _userManager.FindByIdAsync(application.UserId);
        if (user is null)
        {
            return (false, "Applicant no longer exists.");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Instructor);
        if (!roleResult.Succeeded)
        {
            return (false, string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        application.Status = InstructorApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedBy = reviewerId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RejectAsync(int applicationId, string reviewerId, string? reason)
    {
        var application = await _db.Set<InstructorApplication>()
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.Status == InstructorApplicationStatus.Pending);
        if (application is null)
        {
            return (false, "Pending application not found.");
        }

        application.Status = InstructorApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedBy = reviewerId;
        application.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
