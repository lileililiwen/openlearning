using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UserManagement.Services;

/// <summary>Row shown on the admin user list.</summary>
public sealed record UserListItem(
    string UserId,
    string DisplayName,
    string Email,
    bool IsSuspended,
    DateTime CreatedAt,
    List<string> Roles);

/// <summary>Full profile shown on the admin user detail page.</summary>
public sealed record UserDetailItem(
    ApplicationUser User,
    List<string> Roles,
    List<EnrollmentEntity> Enrollments,
    List<Course> Courses);

public class UserManagementService
{
    private readonly DbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(DbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>Users matching the optional name/email search term, with their roles.</summary>
    public async Task<List<UserListItem>> SearchUsersAsync(string? search)
    {
        IQueryable<ApplicationUser> query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.DisplayName.ToLower().Contains(term)
                || (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        var items = new List<UserListItem>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItem(
                user.Id, user.DisplayName, user.Email ?? string.Empty, user.IsSuspended, user.CreatedAt, roles.ToList()));
        }

        return items;
    }

    public async Task<UserDetailItem?> GetUserDetailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var enrollments = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
        var courses = await _db.Set<Course>().AsNoTracking()
            .Where(c => c.InstructorId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return new UserDetailItem(user, roles.ToList(), enrollments, courses);
    }

    /// <summary>Adds or removes a non-Admin role; takes effect on the next request.</summary>
    public async Task<(bool Ok, string? Error)> SetRoleAsync(string userId, string role, bool add)
    {
        if (role != Roles.Student && role != Roles.Instructor)
        {
            return (false, "Only the Student and Instructor roles can be changed.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        var alreadyInRole = await _userManager.IsInRoleAsync(user, role);
        if (add == alreadyInRole)
        {
            return (false, add ? "This user already has that role." : "This user does not have that role.");
        }

        var result = add
            ? await _userManager.AddToRoleAsync(user, role)
            : await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetSuspendedAsync(string userId, bool suspended)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        if (user.IsSuspended == suspended)
        {
            return (false, suspended ? "This user is already suspended." : "This user is not suspended.");
        }

        user.IsSuspended = suspended;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }
}
