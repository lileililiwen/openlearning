using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Gradebook.Models;
using OpenLearning.Gradebook.Services;

namespace OpenLearning.Web.Pages.Courses.Gradebook;

[Authorize(Policy = Policies.RequireInstructor)]
public class IndexModel : PageModel
{
    private readonly GradebookService _gradebook;
    private readonly CourseService _courses;

    public IndexModel(GradebookService gradebook, CourseService courses)
    {
        _gradebook = gradebook;
        _courses = courses;
    }

    public Course? Course { get; set; }

    public GradebookConfig? Config { get; set; }

    public int WeightTotal { get; set; }

    public List<GradebookService.StudentAggregate> Rows { get; set; } = new();

    public Dictionary<string, string> StudentNames { get; set; } = new();

    public List<OpenLearning.Assignments.Models.Assignment> Assignments { get; set; } = new();

    public List<OpenLearning.Assessments.Models.Quiz> Quizzes { get; set; } = new();

    public List<OpenLearning.Exams.Models.Exam> Exams { get; set; } = new();

    [BindProperty] public GradebookItemKind Kind { get; set; }

    [BindProperty] public int SourceId { get; set; }

    [BindProperty] public int Weight { get; set; } = 20;

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var load = await LoadAsync(courseId);
        return load ?? Page();
    }

    public async Task<IActionResult> OnPostAddAsync(int courseId)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _gradebook.AddItemAsync(courseId, Kind, SourceId, Weight, userId, User.IsInRole(Roles.Admin));
        TempData["Message"] = ok ? "Item added." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int courseId, int itemId)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _gradebook.RemoveItemAsync(courseId, itemId, userId, User.IsInRole(Roles.Admin));
        TempData["Message"] = ok ? "Item removed." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostOverrideAsync(
        int courseId, int itemId, string studentId, int? overrideScore)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _gradebook.SetOverrideAsync(itemId, studentId, overrideScore, userId, User.IsInRole(Roles.Admin));
        TempData["Message"] = ok ? "Override saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostExcuseAsync(
        int courseId, int itemId, string studentId, bool excused, string? reason)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _gradebook.SetExcusalAsync(itemId, studentId, excused, reason, userId, User.IsInRole(Roles.Admin));
        var message = excused ? "Student excused from item." : "Excusal removed.";
        TempData["Message"] = ok ? message : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostPublishAsync(int courseId)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var config = await _gradebook.GetConfigAsync(courseId);
        if (config is null)
        {
            return RedirectToPage(new { courseId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _gradebook.PublishAsync(config, userId);
        TempData["Message"] = ok ? "Gradebook published to students." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int courseId)
    {
        if (!await AuthorizeAsync(courseId))
        {
            return Forbid();
        }

        var config = await _gradebook.GetConfigAsync(courseId);
        if (config is null)
        {
            return RedirectToPage(new { courseId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _gradebook.UnpublishAsync(config, userId);
        TempData["Message"] = "Gradebook unpublished.";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { courseId });
    }

    private async Task<bool> AuthorizeAsync(int courseId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        if (course is null)
        {
            return false;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return course.InstructorId == userId || User.IsInRole(Roles.Admin);
    }

    private async Task<IActionResult?> LoadAsync(int courseId)
    {
        var course = await _courses.GetByIdAsync(courseId);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (course.InstructorId != userId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        Course = course;
        Config = await _gradebook.GetConfigAsync(courseId);
        if (Config is not null)
        {
            WeightTotal = Config.Items.Sum(i => i.Weight);
            Rows = await _gradebook.ComputeAsync(Config);
            StudentNames = await _gradebook.GetDisplayNamesAsync(Rows.Select(r => r.StudentId));
        }

        (Assignments, Quizzes, Exams) = await _gradebook.GetCandidatesAsync(courseId);
        return null;
    }
}
