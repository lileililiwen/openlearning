using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;

namespace OpenLearning.Auth.Services;

/// <summary>
/// Period-filtered user signup and role-breakdown queries for the admin
/// reporting pages. Lives in the Auth module so it can use UserManager
/// without dragging in any other module.
/// </summary>
public class UserService
{
    private readonly DbContext _db;

    public UserService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Loads users by their ids (used to resolve submission student names).</summary>
    public Task<List<ApplicationUser?>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        return _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .Cast<ApplicationUser?>()
            .ToListAsync();
    }

    /// <summary>Signups per day in the range.</summary>
    public async Task<List<(DateTime Day, int Count)>> GetSignupsOverTimeAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<ApplicationUser> query = _db.Set<ApplicationUser>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(u => u.CreatedAt < end);
        }

        var rows = await query
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(r => r.Day)
            .ToListAsync();
        return rows.Select(r => (r.Day, r.Count)).ToList();
    }

    /// <summary>Total signups per role within the range.</summary>
    public async Task<List<(string Role, int Count)>> GetSignupsByRoleAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<ApplicationUser> query = _db.Set<ApplicationUser>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(u => u.CreatedAt < end);
        }

        var userIds = await query.Select(u => u.Id).ToListAsync();
        if (userIds.Count == 0)
        {
            return new List<(string, int)>();
        }

        var roleRows = await _db.Set<IdentityUserRole<string>>().AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Set<IdentityRole>().AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { UserId = ur.UserId, Role = r.Name ?? string.Empty })
            .ToListAsync();

        var counts = roleRows
            .GroupBy(row => row.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count());
        return counts
            .Select(kv => (kv.Key, kv.Value))
            .OrderByDescending(kv => kv.Item2)
            .ToList();
    }

    /// <summary>Users in the range with roles, for CSV export.</summary>
    public async Task<List<(ApplicationUser User, string Roles)>> GetUsersForExportAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<ApplicationUser> query = _db.Set<ApplicationUser>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(u => u.CreatedAt < end);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        var roleRows = await _db.Set<IdentityUserRole<string>>().AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Set<IdentityRole>().AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, Role = r.Name ?? string.Empty })
            .ToListAsync();
        var rolesByUser = roleRows
            .GroupBy(rr => rr.UserId)
            .ToDictionary(g => g.Key, g => string.Join(",", g.Select(x => x.Role).OrderBy(x => x)));

        return users
            .Select(u => (u, rolesByUser.TryGetValue(u.Id, out var roles) ? roles : string.Empty))
            .ToList();
    }

    /// <summary>Count of suspended accounts (used by the user report).</summary>
    public Task<int> CountSuspendedAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<ApplicationUser> query = _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.IsSuspended);
        if (rangeFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(u => u.CreatedAt < end);
        }
        return query.CountAsync();
    }

    /// <summary>Total users created in the range.</summary>
    public Task<int> CountSignupsAsync(DateTime? from, DateTime? to)
    {
        var rangeFrom = NormalizeUtc(from);
        var rangeTo = NormalizeUtc(to);

        IQueryable<ApplicationUser> query = _db.Set<ApplicationUser>().AsNoTracking();
        if (rangeFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= rangeFrom.Value);
        }
        if (rangeTo is not null)
        {
            var end = rangeTo.Value.Date.AddDays(1);
            query = query.Where(u => u.CreatedAt < end);
        }
        return query.CountAsync();
    }

    /// <summary>Date-only inputs bind with Kind=Unspecified, which Npgsql rejects for timestamptz.</summary>
    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value is null
                ? null
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }
}
