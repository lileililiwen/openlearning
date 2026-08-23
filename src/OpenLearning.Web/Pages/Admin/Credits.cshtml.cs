using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Credits.Models;
using OpenLearning.Credits.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "学分与毕业")]
public sealed class CreditsModel(
    CreditService credits,
    CourseService courses,
    UserManager<ApplicationUser> users,
    ProgressService progress) : PageModel
{
    public List<GraduationProgram> Programs { get; private set; } = [];
    public List<CourseCreditRule> Rules { get; private set; } = [];
    public List<Course> Courses { get; private set; } = [];
    public List<ApplicationUser> Students { get; private set; } = [];
    public List<(ApplicationUser Student, Course Course)> BackfillPreview { get; private set; } = [];

    public async Task OnGetAsync(bool previewBackfill = false)
    {
        Programs = await credits.ListProgramsAsync();
        Rules = await credits.ListCourseRulesAsync();
        Courses = await courses.GetAllAsync();
        Students = await users.GetUsersInRoleAsync("Student") is { } found ? found.ToList() : [];
        if (previewBackfill)
        {
            var candidates = Students.SelectMany(student => Rules.Where(r => r.IsActive)
                .Select(rule => (student, rule)));
            foreach (var (student, rule) in candidates)
            {
                var course = Courses.FirstOrDefault(c => c.Id == rule.CourseId);
                if (course is not null && await progress.GetProgressPercentAsync(student.Id, course.Id) == 100)
                {
                    BackfillPreview.Add((student, course));
                }
            }
        }
    }

    public async Task<IActionResult> OnPostPublishRuleAsync(int courseId, decimal amount, CreditCategory category)
    {
        await credits.PublishCourseRuleAsync(courseId, amount, category);
        return Success("Course credit rule published.");
    }

    public async Task<IActionResult> OnPostCreateProgramAsync(string name, decimal minTotalCredits,
        string? categoryMinimums, string? requiredCourseIds, int? creditExpiryDays)
    {
        try
        {
            var categories = ParseCategories(categoryMinimums);
            var courseIds = Split(requiredCourseIds);
            await credits.CreateProgramAsync(name, minTotalCredits, categories, courseIds, creditExpiryDays);
        }
        catch (ArgumentException ex)
        {
            return Failure(ex.Message);
        }
        return Success("Program version published.");
    }

    public async Task<IActionResult> OnPostAssignAsync(string studentId, int programId)
    { await credits.AssignProgramAsync(studentId, programId); return Success("Program assigned."); }

    public async Task<IActionResult> OnPostAdjustAsync(string studentId, decimal amount, CreditCategory category, string reason)
    {
        if (amount == 0 || string.IsNullOrWhiteSpace(reason))
            return Failure("A non-zero amount and reason are required.");
        await credits.AwardAsync(studentId, amount, category, "admin-adjustment", Guid.NewGuid().ToString("N"), 1, reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Success("Credit adjustment recorded.");
    }

    public async Task<IActionResult> OnPostRevokeAsync(int awardId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Failure("A revocation reason is required.");
        await credits.RevokeAsync(awardId, reason, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Success("Compensating revocation recorded.");
    }

    public async Task<IActionResult> OnPostGraduateAsync(string studentId, int programId, string? notes)
    {
        try
        { await credits.GraduateAsync(studentId, programId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, notes); return Success("Graduation recorded."); }
        catch (InvalidOperationException ex) { return Failure(ex.Message); }
    }

    public async Task<IActionResult> OnPostBackfillAsync()
    {
        var allStudents = await users.GetUsersInRoleAsync("Student");
        var allRules = await credits.ListCourseRulesAsync();
        var count = 0;
        var candidates = allStudents.SelectMany(student => allRules.Where(r => r.IsActive)
            .Select(rule => (student, rule)));
        foreach (var (student, rule) in candidates)
        {
            if (await progress.GetProgressPercentAsync(student.Id, rule.CourseId) == 100 &&
                await credits.ProcessCourseCompletionAsync(student.Id, rule.CourseId) is not null)
            {
                count++;
            }
        }
        return Success($"Backfill completed: {count} award(s) recorded.");
    }

    private RedirectToPageResult Success(string message) { TempData["Message"] = message; TempData["MessageType"] = "success"; return RedirectToPage(); }
    private RedirectToPageResult Failure(string message) { TempData["Message"] = message; TempData["MessageType"] = "danger"; return RedirectToPage(); }
    private static List<string> Split(string? value)
    {
        return (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static Dictionary<CreditCategory, decimal> ParseCategories(string? value)
    {
        var result = new Dictionary<CreditCategory, decimal>();
        foreach (var part in Split(value))
        {
            var pair = part.Split(':', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2 || !Enum.TryParse(pair[0], true, out CreditCategory category) ||
                !decimal.TryParse(pair[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
                throw new ArgumentException("Category minimums must use Category:Amount entries.");
            result[category] = amount;
        }
        return result;
    }
}
