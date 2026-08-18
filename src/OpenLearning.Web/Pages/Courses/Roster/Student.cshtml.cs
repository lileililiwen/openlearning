using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;

namespace OpenLearning.Web.Pages.Courses.Roster;

[Authorize(Policy = Policies.RequireInstructor)]
public class StudentModel : PageModel
{
    private readonly CourseService _courses;
    private readonly ModuleService _modules;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly AttemptService _attempts;
    private readonly ScormRuntimeService _scorm;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentModel(
        CourseService courses,
        ModuleService modules,
        EnrollmentService enrollments,
        ProgressService progress,
        AttemptService attempts,
        ScormRuntimeService scorm,
        UserManager<ApplicationUser> userManager)
    {
        _courses = courses;
        _modules = modules;
        _enrollments = enrollments;
        _progress = progress;
        _attempts = attempts;
        _scorm = scorm;
        _userManager = userManager;
    }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public DateTime EnrolledAt { get; set; }

    public int ProgressPercent { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public List<LessonRow> Lessons { get; set; } = new();

    public List<QuizRow> Quizzes { get; set; } = new();

    public List<ScormRow> Scorm { get; set; } = new();

    public sealed record LessonRow(int Id, string Title, bool IsCompleted);

    public sealed record QuizRow(int Id, string Title, int Attempts, int BestPercent);

    public sealed record ScormRow(int Id, string Title, string Status, string ScoreRaw, DateTime UpdatedAt);

    public async Task<IActionResult> OnGetAsync(int id, string studentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _courses.IsOwnerAsync(id, userId))
        {
            return Forbid();
        }

        var course = await _courses.GetByIdAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        var student = await _userManager.FindByIdAsync(studentId);
        if (student is null)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(studentId, id))
        {
            return NotFound();
        }

        var enrollment = await _enrollments.GetEnrollmentsForRosterAsync(id);
        var enr = enrollment.Enrollments.FirstOrDefault(e => e.StudentId == studentId);
        if (enr is null)
        {
            return NotFound();
        }

        CourseId = id;
        CourseTitle = course.Title;
        Student = student;
        EnrolledAt = enr.EnrolledAt;
        var completed = await _progress.GetCompletedLessonIdsAsync(studentId, id);
        ProgressPercent = enrollment.TotalLessons == 0
            ? 0
            : (int)Math.Round(completed.Count * 100.0 / enrollment.TotalLessons);

        // Lessons across all modules in order
        var modules = await _modules.GetForCourseAsync(id);
        var lessonRows = new List<LessonRow>();
        foreach (var module in modules)
        {
            var moduleLessons = await _modules.GetLessonsAsync(module.Id);
            lessonRows.AddRange(moduleLessons.Select(l => new LessonRow(l.Id, l.Title, completed.Contains(l.Id))));
        }
        Lessons = lessonRows;
        LastAccessedAt = await _progress.GetLastAccessAsync(studentId, id);

        Quizzes = (await _attempts.GetQuizzesWithAttemptsForStudentAsync(studentId, id))
            .Select(q => new QuizRow(q.Id, q.Title, q.Attempts, q.BestPercent))
            .ToList();

        Scorm = (await _scorm.GetRecordsForEnrollmentAsync(enr.Id))
            .Select(r => new ScormRow(
                r.Id,
                r.ScormPackage?.Title ?? $"Package #{r.ScormPackageId}",
                string.IsNullOrEmpty(r.LessonStatus) ? "not attempted" : r.LessonStatus,
                r.ScoreRaw,
                r.UpdatedAt))
            .ToList();

        return Page();
    }
}
