using Microsoft.EntityFrameworkCore;
using OpenLearning.Logging.Models;

namespace OpenLearning.Logging.Services;

/// <summary>
/// Records operation (audit) and error entries. Best-effort: call sites record
/// mutations; the exception middleware persists unhandled exceptions.
/// </summary>
public class LogService
{
    private readonly DbContext _db;

    public LogService(DbContext db)
    {
        _db = db;
    }

    public Task RecordAsync(
        string? actorId,
        string actorName,
        string action,
        string? targetType,
        string? targetId,
        string? details,
        string? ipAddress)
    {
        _db.Set<OperationLog>().Add(new OperationLog
        {
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            IpAddress = ipAddress,
        });
        return _db.SaveChangesAsync();
    }

    public Task LogErrorAsync(string message, string? stackTrace, string? path, string? requestMethod, string? userId)
    {
        _db.Set<ErrorLog>().Add(new ErrorLog
        {
            Message = message,
            StackTrace = stackTrace,
            Path = path,
            RequestMethod = requestMethod,
            UserId = userId,
        });
        return _db.SaveChangesAsync();
    }

    /// <summary>Admin operations query with filters and pagination.</summary>
    public async Task<(List<OperationLog> Items, int Total)> GetOperationsAsync(
        string? action, string? actor, DateTime? from, DateTime? to, int page, int pageSize)
    {
        IQueryable<OperationLog> query = _db.Set<OperationLog>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            var term = actor.Trim().ToLowerInvariant();
            // The analyzer suggestions (StringComparison overload, culture-aware
            // ToLower) are not translatable by EF Core, so we lowercase both
            // sides instead
#pragma warning disable CA1862, CA1304, CA1311
            query = query.Where(l => l.ActorName.ToLower().Contains(term));
#pragma warning restore CA1862, CA1304, CA1311
        }

        if (from is not null)
        {
            query = query.Where(l => l.CreatedAt >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        }

        if (to is not null)
        {
            query = query.Where(l => l.CreatedAt < DateTime.SpecifyKind(to.Value, DateTimeKind.Utc).AddDays(1));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    /// <summary>Admin error query with filters and pagination.</summary>
    public async Task<(List<ErrorLog> Items, int Total)> GetErrorsAsync(
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        IQueryable<ErrorLog> query = _db.Set<ErrorLog>().AsNoTracking();
        if (from is not null)
        {
            query = query.Where(l => l.CreatedAt >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        }

        if (to is not null)
        {
            query = query.Where(l => l.CreatedAt < DateTime.SpecifyKind(to.Value, DateTimeKind.Utc).AddDays(1));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    /// <summary>Deletes entries older than the retention period.</summary>
    public async Task<int> PruneAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var oldOperations = await _db.Set<OperationLog>()
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync();
        var oldErrors = await _db.Set<ErrorLog>()
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync();
        if (oldOperations.Count == 0 && oldErrors.Count == 0)
        {
            return 0;
        }

        _db.Set<OperationLog>().RemoveRange(oldOperations);
        _db.Set<ErrorLog>().RemoveRange(oldErrors);
        await _db.SaveChangesAsync();
        return oldOperations.Count + oldErrors.Count;
    }
}
