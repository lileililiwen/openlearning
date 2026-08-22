using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenLearning.StudyTools.Models;

namespace OpenLearning.StudyTools.Services;

public sealed record NoteInput(
    string Body,
    NoteContextType ContextType,
    int ContextId,
    int? MediaOffsetSeconds,
    string? Tags);

public sealed record NoteExportEntry(
    int Id,
    string Body,
    NoteContextType ContextType,
    int ContextId,
    int? MediaOffsetSeconds,
    string? Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class LearnerNoteService
{
    private readonly DbContext _db;

    public LearnerNoteService(DbContext db)
    {
        _db = db;
    }

    public async Task<(int Id, string? Error)> CreateAsync(string userId, NoteInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Body))
        {
            return (0, "Note body is required.");
        }

        if (!await IsContextAccessibleAsync(input.ContextType, input.ContextId))
        {
            return (0, "Context not found or not accessible.");
        }

        var note = new LearnerNote
        {
            UserId = userId,
            Body = SanitizeMarkdown(input.Body.Trim()),
            ContextType = input.ContextType,
            ContextId = input.ContextId,
            MediaOffsetSeconds = input.MediaOffsetSeconds,
            Tags = NormalizeTags(input.Tags),
        };
        _db.Set<LearnerNote>().Add(note);
        await _db.SaveChangesAsync();
        return (note.Id, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(string userId, int noteId, NoteInput input)
    {
        var note = await _db.Set<LearnerNote>()
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);
        if (note is null)
        {
            return (false, "Note not found.");
        }

        if (string.IsNullOrWhiteSpace(input.Body))
        {
            return (false, "Note body is required.");
        }

        if (!await IsContextAccessibleAsync(input.ContextType, input.ContextId))
        {
            return (false, "Context not found or not accessible.");
        }

        note.Body = SanitizeMarkdown(input.Body.Trim());
        note.ContextType = input.ContextType;
        note.ContextId = input.ContextId;
        note.MediaOffsetSeconds = input.MediaOffsetSeconds;
        note.Tags = NormalizeTags(input.Tags);
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(string userId, int noteId)
    {
        var note = await _db.Set<LearnerNote>()
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);
        if (note is null)
        {
            return false;
        }

        _db.Set<LearnerNote>().Remove(note);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<LearnerNote?> GetByIdAsync(string userId, int noteId)
    {
        return _db.Set<LearnerNote>().AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);
    }

    public async Task<List<LearnerNote>> ListAsync(
        string userId, NoteContextType? contextType = null, int? contextId = null,
        string? tag = null, string? search = null)
    {
        var query = _db.Set<LearnerNote>().AsNoTracking()
            .Where(n => n.UserId == userId);

        if (contextType.HasValue)
        {
            query = query.Where(n => n.ContextType == contextType.Value);
        }

        if (contextId.HasValue)
        {
            query = query.Where(n => n.ContextId == contextId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(n => n.Tags != null && n.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.Body.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return await query.OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public async Task<List<NoteExportEntry>> ExportAsync(string userId)
    {
        return await _db.Set<LearnerNote>().AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.ContextType).ThenBy(n => n.ContextId).ThenBy(n => n.CreatedAt)
            .Select(n => new NoteExportEntry(
                n.Id, n.Body, n.ContextType, n.ContextId,
                n.MediaOffsetSeconds, n.Tags, n.CreatedAt, n.UpdatedAt))
            .ToListAsync();
    }

    public static string RenderExportCsv(IEnumerable<NoteExportEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Body,ContextType,ContextId,MediaOffsetSeconds,Tags,CreatedAt,UpdatedAt");
        foreach (var e in entries)
        {
            sb.Append(e.Id).Append(',');
            sb.Append(CsvEscape(e.Body)).Append(',');
            sb.Append(e.ContextType).Append(',');
            sb.Append(e.ContextId).Append(',');
            sb.Append(e.MediaOffsetSeconds?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',');
            sb.Append(CsvEscape(e.Tags ?? "")).Append(',');
            sb.Append(e.CreatedAt.ToString("o", CultureInfo.InvariantCulture)).Append(',');
            sb.AppendLine(e.UpdatedAt.ToString("o", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private async Task<bool> IsContextAccessibleAsync(NoteContextType contextType, int contextId)
    {
        return contextType switch
        {
            NoteContextType.Course => await _db.Set<OpenLearning.CourseManagement.Models.Course>()
                .AnyAsync(c => c.Id == contextId),
            NoteContextType.Lesson => await _db.Set<OpenLearning.CourseManagement.Models.Lesson>()
                .AnyAsync(l => l.Id == contextId),
            NoteContextType.Resource => await _db.Set<OpenLearning.CourseManagement.Models.Lesson>()
                .AnyAsync(l => l.Id == contextId),
            _ => false,
        };
    }

    /// <summary>
    /// Strip HTML tags and dangerous content, keep Markdown syntax.
    /// </summary>
    public static string SanitizeMarkdown(string markdown)
    {
        // Remove HTML tags
        var clean = Regex.Replace(markdown, "<[^>]+>", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(2));
        // Remove script/event handlers that might survive
        clean = Regex.Replace(clean, "javascript:", string.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        clean = Regex.Replace(clean, "on\\w+\\s*=", string.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        return clean.Trim();
    }

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var parts = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= 50)
            .Distinct()
            .Take(10);
        return string.Join(",", parts);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }
}
