using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.LearningPaths.Models;
using OpenLearning.Progress.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.LearningPaths.Services;

public enum PathCourseState { Blocked, Available, PurchaseRequired, InProgress, Complete }

public sealed record PathCourseProgress(int CourseId, string Title, decimal? Price, bool IsRequired,
    int StagePosition, int Position, PathCourseState State, int ProgressPercent, int? PrerequisiteCourseId);
public sealed record PathProgress(int EnrollmentId, int PathId, string Title, int VersionNumber,
    bool IsComplete, DateTime? CompletedAt, IReadOnlyList<PathCourseProgress> Courses);

public sealed class LearningPathService
{
    private readonly DbContext _db;
    public LearningPathService(DbContext db)
    {
        _db = db;
    }

    public Task<List<LearningPath>> ListManagedAsync(string userId, bool isAdmin)
    {
        return _db.Set<LearningPath>()
        .AsNoTracking().Where(x => isAdmin || x.OwnerId == userId).OrderBy(x => x.Title).ToListAsync();
    }

    public Task<List<LearningPath>> CatalogAsync()
    {
        return _db.Set<LearningPath>().AsNoTracking()
        .Where(x => !x.IsArchived && x.Versions.Any(v => v.IsPublished))
        .Include(x => x.Versions.Where(v => v.IsPublished)).OrderBy(x => x.Title).ToListAsync();
    }

    public async Task<LearningPath> CreateAsync(string ownerId, string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");
        var path = new LearningPath { OwnerId = ownerId, Title = title.Trim(), Description = description?.Trim() ?? string.Empty };
        path.Versions.Add(new LearningPathVersion { VersionNumber = 1 });
        _db.Add(path);
        await _db.SaveChangesAsync();
        return path;
    }

    public async Task<LearningPathVersion?> GetDraftAsync(int pathId, string userId, bool isAdmin)
    {
        return await _db.Set<LearningPathVersion>().Include(x => x.LearningPath).Include(x => x.Stages).ThenInclude(x => x.Courses)
                .Where(x => x.LearningPathId == pathId && !x.IsPublished && (isAdmin || x.LearningPath!.OwnerId == userId))
                .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
    }

    public async Task<(bool Ok, string? Error)> AddStageAsync(int pathId, string userId, bool isAdmin,
        string title, int minimumElectives)
    {
        var draft = await GetDraftAsync(pathId, userId, isAdmin);
        if (draft is null)
            return (false, "Draft not found or access denied.");
        if (string.IsNullOrWhiteSpace(title) || minimumElectives < 0)
            return (false, "Valid stage title and elective minimum are required.");
        draft.Stages.Add(new LearningPathStage
        {
            Title = title.Trim(),
            MinimumElectives = minimumElectives,
            Position = draft.Stages.Count == 0 ? 1 : draft.Stages.Max(x => x.Position) + 1
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> AddCourseAsync(int pathId, int stageId, string userId, bool isAdmin,
        int courseId, bool isRequired, int? prerequisiteCourseId)
    {
        var draft = await GetDraftAsync(pathId, userId, isAdmin);
        var stage = draft?.Stages.SingleOrDefault(x => x.Id == stageId);
        if (stage is null)
            return (false, "Draft stage not found or access denied.");
        if (!await _db.Set<Course>().AnyAsync(x => x.Id == courseId))
            return (false, "Course does not exist.");
        if (prerequisiteCourseId == courseId)
            return (false, "A course cannot require itself.");
        if (prerequisiteCourseId is not null && !draft!.Stages.SelectMany(x => x.Courses).Any(x => x.CourseId == prerequisiteCourseId))
            return (false, "The prerequisite must already be in this path.");
        if (draft!.Stages.SelectMany(x => x.Courses).Any(x => x.CourseId == courseId))
            return (false, "Course is already in this path.");
        stage.Courses.Add(new LearningPathCourse
        {
            CourseId = courseId,
            IsRequired = isRequired,
            PrerequisiteCourseId = prerequisiteCourseId,
            Position = stage.Courses.Count == 0 ? 1 : stage.Courses.Max(x => x.Position) + 1
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, int? PublishedVersion)> PublishAsync(int pathId, string userId, bool isAdmin)
    {
        var draft = await GetDraftAsync(pathId, userId, isAdmin);
        if (draft is null)
            return (false, "Draft not found or access denied.", null);
        var error = await ValidateAsync(draft);
        if (error is not null)
            return (false, error, null);
        draft.IsPublished = true;
        draft.PublishedAt = DateTime.UtcNow;
        var next = CloneVersion(draft, draft.VersionNumber + 1);
        _db.Add(next);
        await _db.SaveChangesAsync();
        return (true, null, draft.VersionNumber);
    }

    public async Task<(bool Ok, string? Error)> ArchiveAsync(int pathId, string userId, bool isAdmin)
    {
        var path = await _db.Set<LearningPath>().SingleOrDefaultAsync(x => x.Id == pathId);
        if (path is null || (!isAdmin && path.OwnerId != userId))
            return (false, "Path not found or access denied.");
        path.IsArchived = true;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, PathEnrollment? Enrollment)> EnrollAsync(int pathId, string studentId)
    {
        var version = await _db.Set<LearningPathVersion>().Include(x => x.LearningPath)
            .Where(x => x.LearningPathId == pathId && x.IsPublished && !x.LearningPath!.IsArchived)
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        if (version is null)
            return (false, "Published path not found.", null);
        var existing = await _db.Set<PathEnrollment>().SingleOrDefaultAsync(x => x.StudentId == studentId && x.LearningPathVersionId == version.Id);
        if (existing is not null)
            return (true, null, existing);
        var enrollment = new PathEnrollment { StudentId = studentId, LearningPathVersionId = version.Id };
        _db.Add(enrollment);
        await _db.SaveChangesAsync();
        return (true, null, enrollment);
    }

    public async Task<PathProgress?> GetProgressAsync(int enrollmentId, string studentId)
    {
        var enrollment = await _db.Set<PathEnrollment>().Include(x => x.Version).ThenInclude(x => x!.LearningPath)
            .Include(x => x.Version).ThenInclude(x => x!.Stages).ThenInclude(x => x.Courses)
            .SingleOrDefaultAsync(x => x.Id == enrollmentId && x.StudentId == studentId);
        if (enrollment?.Version?.LearningPath is null)
            return null;
        var items = enrollment.Version.Stages.SelectMany(stage => stage.Courses.Select(item => (stage, item))).ToList();
        var courseIds = items.Select(x => x.item.CourseId).ToList();
        var courses = await _db.Set<Course>().AsNoTracking().Where(x => courseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var active = await _db.Set<EnrollmentEntity>().AsNoTracking().Where(x => x.StudentId == studentId && courseIds.Contains(x.CourseId)
            && x.RevokedAt == null && (x.AccessExpiresAt == null || x.AccessExpiresAt > DateTime.UtcNow)).ToListAsync();
        var enrollmentByCourse = active.GroupBy(x => x.CourseId).ToDictionary(x => x.Key, x => x.OrderByDescending(e => e.EnrolledAt).First());
        var completed = new HashSet<int>();
        var percents = new Dictionary<int, int>();
        foreach (var courseId in courseIds.Distinct())
        {
            if (!enrollmentByCourse.TryGetValue(courseId, out var courseEnrollment))
            { percents[courseId] = 0; continue; }
            var total = await _db.Set<Module>().Where(x => x.CourseId == courseId).SelectMany(x => x.Lessons).CountAsync();
            var done = await _db.Set<LessonCompletion>().CountAsync(x => x.EnrollmentId == courseEnrollment.Id);
            percents[courseId] = total == 0 ? 0 : Math.Min(100, (int)Math.Round(done * 100.0 / total));
            if (total > 0 && done >= total)
                completed.Add(courseId);
        }
        var progress = items.OrderBy(x => x.stage.Position).ThenBy(x => x.item.Position).Select(x =>
        {
            var course = courses[x.item.CourseId];
            var blocked = x.item.PrerequisiteCourseId is int prerequisite && !completed.Contains(prerequisite);
            var state = GetCourseState(blocked, completed.Contains(course.Id),
                enrollmentByCourse.ContainsKey(course.Id), course.IsFree);
            return new PathCourseProgress(course.Id, course.Title, course.Price, x.item.IsRequired, x.stage.Position,
                x.item.Position, state, percents[course.Id], x.item.PrerequisiteCourseId);
        }).ToList();
        var complete = enrollment.Version.Stages.All(stage =>
            stage.Courses.Where(x => x.IsRequired).All(x => completed.Contains(x.CourseId)) &&
            stage.Courses.Count(x => !x.IsRequired && completed.Contains(x.CourseId)) >= stage.MinimumElectives);
        if (complete && enrollment.CompletedAt is null)
        { enrollment.CompletedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return new PathProgress(enrollment.Id, enrollment.Version.LearningPathId, enrollment.Version.LearningPath.Title,
            enrollment.Version.VersionNumber, complete, enrollment.CompletedAt, progress);
    }

    public async Task<List<PathEnrollment>> ListEnrollmentsAsync(string studentId)
    {
        return await _db.Set<PathEnrollment>()
        .AsNoTracking().Include(x => x.Version).ThenInclude(x => x!.LearningPath).Where(x => x.StudentId == studentId)
        .OrderByDescending(x => x.EnrolledAt).ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> MigrateEnrollmentAsync(int enrollmentId, int versionId, string userId, bool isAdmin)
    {
        var enrollment = await _db.Set<PathEnrollment>().Include(x => x.Version).ThenInclude(x => x!.LearningPath).SingleOrDefaultAsync(x => x.Id == enrollmentId);
        var target = await _db.Set<LearningPathVersion>().SingleOrDefaultAsync(x => x.Id == versionId && x.IsPublished);
        if (enrollment?.Version?.LearningPath is null || target is null || target.LearningPathId != enrollment.Version.LearningPathId ||
            (!isAdmin && enrollment.Version.LearningPath.OwnerId != userId))
            return (false, "Enrollment or compatible published version not found.");
        enrollment.LearningPathVersionId = target.Id;
        enrollment.CompletedAt = null;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task<string?> ValidateAsync(LearningPathVersion version)
    {
        var items = version.Stages.SelectMany(x => x.Courses).ToList();
        if (version.Stages.Count == 0 || items.Count == 0)
            return "At least one non-empty stage is required.";
        if (version.Stages.Any(x => x.MinimumElectives > x.Courses.Count(c => !c.IsRequired)))
            return "An elective minimum exceeds the available elective courses.";
        var ids = items.Select(x => x.CourseId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            return "A course can appear only once.";
        if (await _db.Set<Course>().CountAsync(x => ids.Contains(x.Id) && x.Status == CourseStatus.Published) != ids.Count)
            return "Every referenced course must exist and be published.";
        var edges = items.Where(x => x.PrerequisiteCourseId is not null).ToDictionary(x => x.CourseId, x => x.PrerequisiteCourseId!.Value);
        foreach (var start in edges.Keys)
        { var seen = new HashSet<int>(); var current = start; while (edges.TryGetValue(current, out current)) if (!seen.Add(current)) return $"Prerequisite cycle detected at course {current}."; }
        return null;
    }

    private static PathCourseState GetCourseState(bool blocked, bool complete, bool enrolled, bool free)
    {
        if (blocked)
            return PathCourseState.Blocked;
        if (complete)
            return PathCourseState.Complete;
        if (enrolled)
            return PathCourseState.InProgress;
        return free ? PathCourseState.Available : PathCourseState.PurchaseRequired;
    }

    private static LearningPathVersion CloneVersion(LearningPathVersion source, int number)
    {
        return new()
        {
            LearningPathId = source.LearningPathId,
            VersionNumber = number,
            Stages = source.Stages.OrderBy(x => x.Position).Select(stage => new LearningPathStage
            {
                Title = stage.Title,
                Position = stage.Position,
                MinimumElectives = stage.MinimumElectives,
                Courses = stage.Courses.OrderBy(x => x.Position).Select(item => new LearningPathCourse
                { CourseId = item.CourseId, IsRequired = item.IsRequired, Position = item.Position, PrerequisiteCourseId = item.PrerequisiteCourseId }).ToList()
            }).ToList()
        };
    }
}
