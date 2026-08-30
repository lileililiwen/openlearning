using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Navigation.Models;
using OpenLearning.Outcomes.Models;
using OpenLearning.Outcomes.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Web.Pages.Courses.Outcomes;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
[Breadcrumb("首页:/", "教师工作台:/Dashboard/Teacher", "课程成果与掌握度")]
public class IndexModel : PageModel
{
    private readonly OutcomeOperationsService _outcomes;
    private readonly ApplicationDbContext _db;

    public IndexModel(OutcomeOperationsService outcomes, ApplicationDbContext db)
    {
        _outcomes = outcomes;
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public List<(CourseOutcome Outcome, List<OutcomeActivityMapping> Mappings)> Outcomes { get; set; } = new();

    public List<LearnerOutcomeRow> LearnerRows { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        Outcomes = await _outcomes.GetOutcomesWithMappingsAsync(CourseId);
        await BuildLearnerRowsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateOutcomeAsync(string title, string description, double threshold)
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course!;
        var result = await _outcomes.CreateOutcomeAsync(CourseId, course.InstructorId, new CreateOutcomeRequest
        {
            Title = title,
            Description = description,
            MasteryThreshold = threshold,
        });

        if (!result.Succeeded)
        {
            TempData["Message"] = string.Join(" ", result.Errors.Select(e => e.Message));
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        TempData["Message"] = "已添加课程成果。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostAddMappingAsync(int outcomeId, string activityType, int activityId, double weight)
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course!;
        var result = await _outcomes.AddOutcomeActivityMappingAsync(CourseId, course.InstructorId, new AddMappingRequest
        {
            OutcomeId = outcomeId,
            ActivityType = activityType,
            ActivityId = activityId,
            Weight = weight,
        });

        if (!result.Succeeded)
        {
            TempData["Message"] = string.Join(" ", result.Errors.Select(e => e.Message));
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { CourseId });
        }

        TempData["Message"] = "已将活动映射到成果。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { CourseId });
    }

    public async Task<IActionResult> OnPostRecalculateAsync(string learnerId)
    {
        var access = await EnsureAccessAsync();
        if (access is not null)
        {
            return access;
        }

        var course = Course!;
        var outcomes = await _outcomes.GetOutcomesWithMappingsAsync(CourseId);
        foreach (var (outcome, _) in outcomes)
        {
            await _outcomes.RecalculateAsync(CourseId, outcome.Id, learnerId, course.InstructorId);
        }

        TempData["Message"] = "已为该学员重新计算掌握度。";
        TempData["MessageType"] = "info";
        return RedirectToPage(new { CourseId });
    }

    private async Task BuildLearnerRowsAsync()
    {
        var enrollments = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.CourseId == CourseId && e.RevokedAt == null)
            .Select(e => e.StudentId)
            .ToListAsync();

        if (enrollments.Count == 0)
        {
            return;
        }

        var users = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => enrollments.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        foreach (var learnerId in enrollments)
        {
            var cells = new List<OutcomeCell>();
            foreach (var (outcome, _) in Outcomes)
            {
                var evaluation = await _outcomes.EvaluateAsync(CourseId, outcome.Id, learnerId);
                cells.Add(new OutcomeCell
                {
                    OutcomeId = outcome.Id,
                    OutcomeTitle = outcome.Title,
                    State = evaluation.Mastery.State,
                    Score = evaluation.Mastery.Score,
                    Signals = evaluation.Signals,
                });
            }

            var user = users.GetValueOrDefault(learnerId);
            var name = LearnerDisplayName(user, learnerId);

            LearnerRows.Add(new LearnerOutcomeRow
            {
                LearnerId = learnerId,
                LearnerName = name,
                Cells = cells,
            });
        }
    }

    private async Task<IActionResult?> EnsureAccessAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CourseId);
        if (course is null)
        {
            return NotFound();
        }

        if (course.InstructorId != userId && !User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        Course = course;
        return null;
    }

    private static string LearnerDisplayName(ApplicationUser? user, string learnerId)
    {
        if (user is null)
        {
            return learnerId;
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(user.RealName))
        {
            return user.RealName;
        }

        return user.Email ?? learnerId;
    }

    public static string BadgeClass(MasteryState state)
    {
        if (state == MasteryState.Mastered)
        {
            return "success";
        }

        if (state == MasteryState.NeedsReview)
        {
            return "warning";
        }

        if (state == MasteryState.Developing)
        {
            return "info";
        }

        return "secondary";
    }

    public static string StateLabel(MasteryState state)
    {
        if (state == MasteryState.Mastered)
        {
            return "已掌握";
        }

        if (state == MasteryState.NeedsReview)
        {
            return "需关注";
        }

        if (state == MasteryState.Developing)
        {
            return "进行中";
        }

        return "未开始";
    }
}

public sealed class LearnerOutcomeRow
{
    public string LearnerId { get; set; } = string.Empty;

    public string LearnerName { get; set; } = string.Empty;

    public List<OutcomeCell> Cells { get; set; } = new();
}

public sealed class OutcomeCell
{
    public int OutcomeId { get; set; }

    public string OutcomeTitle { get; set; } = string.Empty;

    public MasteryState State { get; set; }

    public double Score { get; set; }

    public List<InterventionSignal> Signals { get; set; } = new();
}
