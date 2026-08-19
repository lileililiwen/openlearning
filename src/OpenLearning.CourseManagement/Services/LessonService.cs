using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

public class LessonService
{
    private readonly DbContext _db;

    public LessonService(DbContext db)
    {
        _db = db;
    }

    public Task<Lesson?> GetByIdAsync(int id)
    {
        return _db.Set<Lesson>().AsNoTracking()
                .Include(l => l.Module!).ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == id);
    }

    public Task<bool> IsOwnerAsync(int lessonId, string userId)
    {
        return _db.Set<Lesson>().AsNoTracking()
                .AnyAsync(l => l.Id == lessonId && l.Module!.Course!.InstructorId == userId);
    }

    public async Task<Lesson?> AddAsync(
        int moduleId,
        string ownerId,
        string title,
        string content,
        string? videoUrl = null,
        string? videoPosterUrl = null,
        string? subtitleUrl = null,
        bool isPreview = false)
    {
        var module = await _db.Set<Module>()
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module?.Course is null || module.Course.InstructorId != ownerId)
        {
            return null;
        }

        var nextOrder = await _db.Set<Lesson>()
            .Where(l => l.ModuleId == moduleId)
            .Select(l => (int?)l.OrderIndex)
            .MaxAsync() ?? 0;

        var lesson = new Lesson
        {
            ModuleId = moduleId,
            Title = title,
            Content = content,
            VideoUrl = NullIfBlank(videoUrl),
            VideoPosterUrl = NullIfBlank(videoPosterUrl),
            SubtitleUrl = NullIfBlank(subtitleUrl),
            IsPreview = isPreview,
            OrderIndex = nextOrder + 1,
        };
        _db.Set<Lesson>().Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    public async Task<bool> UpdateAsync(
        int lessonId,
        string ownerId,
        string title,
        string content,
        string? videoUrl = null,
        string? videoPosterUrl = null,
        string? subtitleUrl = null,
        bool isPreview = false)
    {
        var lesson = await _db.Set<Lesson>()
            .Include(l => l.Module).ThenInclude(m => m!.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson?.Module?.Course is null || lesson.Module.Course.InstructorId != ownerId)
        {
            return false;
        }

        lesson.Title = title;
        lesson.Content = content;
        lesson.VideoUrl = NullIfBlank(videoUrl);
        lesson.VideoPosterUrl = NullIfBlank(videoPosterUrl);
        lesson.SubtitleUrl = NullIfBlank(subtitleUrl);
        lesson.IsPreview = isPreview;
        await _db.SaveChangesAsync();
        return true;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public async Task<bool> DeleteAsync(int lessonId, string ownerId)
    {
        var lesson = await _db.Set<Lesson>()
            .Include(l => l.Module).ThenInclude(m => m!.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson?.Module?.Course is null || lesson.Module.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<Lesson>().Remove(lesson);
        await _db.SaveChangesAsync();
        return true;
    }
}
