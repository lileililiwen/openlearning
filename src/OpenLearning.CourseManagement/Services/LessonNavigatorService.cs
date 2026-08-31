using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.CourseManagement.Services;

public sealed record LessonSummary(int Id, string Title, bool IsCompleted);

public sealed record ModuleOutline(string ModuleTitle, IReadOnlyList<LessonSummary> Lessons);

public sealed record NavigationIds(int? PrevLessonId, int? NextLessonId);

public class LessonNavigatorService
{
    private readonly DbContext _db;

    public LessonNavigatorService(DbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ModuleOutline>> GetOrderedLessonsAsync(int courseId, HashSet<int>? completedLessonIds = null)
    {
        var modules = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .Select(m => new
            {
                m.Title,
                Lessons = m.Lessons
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new { l.Id, l.Title })
                    .ToList(),
            })
            .ToListAsync();

        var completed = completedLessonIds ?? new HashSet<int>();

        return modules.Select(m => new ModuleOutline(
            m.Title,
            m.Lessons.Select(l => new LessonSummary(l.Id, l.Title, completed.Contains(l.Id))).ToList()))
            .ToList();
    }

    public async Task<NavigationIds> GetNavigatorAsync(int courseId, int currentLessonId)
    {
        var allLessonIds = await _db.Set<Module>().AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .SelectMany(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .Select(l => l.Id)
            .ToListAsync();

        var index = allLessonIds.IndexOf(currentLessonId);
        if (index < 0)
        {
            return new NavigationIds(null, null);
        }

        var prev = index > 0 ? (int?)allLessonIds[index - 1] : null;
        var next = index < allLessonIds.Count - 1 ? (int?)allLessonIds[index + 1] : null;
        return new NavigationIds(prev, next);
    }
}
