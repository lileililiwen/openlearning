using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.LearningPaths.Models;
using OpenLearning.LearningPaths.Services;

namespace OpenLearning.Web.Pages.LearningPaths;

[Authorize(Policy = Policies.RequireInstructorOrAdmin)]
public sealed class ManageModel : PageModel
{
    private readonly LearningPathService _paths;
    private readonly DbContext _db;
    public ManageModel(LearningPathService paths, DbContext db) { _paths = paths; _db = db; }
    public List<LearningPath> Paths { get; private set; } = new();
    public LearningPathVersion? Draft { get; private set; }
    public List<Course> Courses { get; private set; } = new();
    public async Task OnGetAsync(int? id)
    {
        var (userId, admin) = Actor();
        Paths = await _paths.ListManagedAsync(userId, admin);
        if (id is not null)
            Draft = await _paths.GetDraftAsync(id.Value, userId, admin);
        Courses = await _db.Set<Course>().AsNoTracking().Where(x => x.Status == CourseStatus.Published).OrderBy(x => x.Title).ToListAsync();
    }
    public async Task<IActionResult> OnPostCreateAsync(string title, string? description)
    {
        try
        { var path = await _paths.CreateAsync(Actor().UserId, title, description); return RedirectToPage(new { id = path.Id }); }
        catch (ArgumentException ex) { Flash(ex.Message, false); return RedirectToPage(); }
    }
    public async Task<IActionResult> OnPostStageAsync(int id, string title, int minimumElectives)
    { var actor = Actor(); var result = await _paths.AddStageAsync(id, actor.UserId, actor.Admin, title, minimumElectives); Flash(result.Error ?? "Stage added.", result.Ok); return RedirectToPage(new { id }); }
    public async Task<IActionResult> OnPostCourseAsync(int id, int stageId, int courseId, bool isRequired, int? prerequisiteCourseId)
    { var actor = Actor(); var result = await _paths.AddCourseAsync(id, stageId, actor.UserId, actor.Admin, courseId, isRequired, prerequisiteCourseId); Flash(result.Error ?? "Course added.", result.Ok); return RedirectToPage(new { id }); }
    public async Task<IActionResult> OnPostPublishAsync(int id)
    { var actor = Actor(); var result = await _paths.PublishAsync(id, actor.UserId, actor.Admin); Flash(result.Error ?? $"Version {result.PublishedVersion} published.", result.Ok); return RedirectToPage(new { id }); }
    public async Task<IActionResult> OnPostArchiveAsync(int id)
    { var actor = Actor(); var result = await _paths.ArchiveAsync(id, actor.UserId, actor.Admin); Flash(result.Error ?? "Path archived.", result.Ok); return RedirectToPage(); }
    private (string UserId, bool Admin) Actor()
    {
        return (User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole(Roles.Admin));
    }

    private void Flash(string message, bool ok) { TempData["Message"] = message; TempData["MessageType"] = ok ? "success" : "danger"; }
}
