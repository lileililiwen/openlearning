using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Courses.Classes;

[Authorize(Policy = Policies.RequireInstructor)]
public class ManageModel : PageModel
{
    private readonly ClassGroupService _classes;
    private readonly ClassAssignmentService _assignments;
    private readonly NotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageModel(
        ClassGroupService classes,
        ClassAssignmentService assignments,
        NotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _classes = classes;
        _assignments = assignments;
        _notifications = notifications;
        _userManager = userManager;
    }

    public ClassGroup? ClassGroup { get; set; }

    public List<ClassAssignment> Assignments { get; set; } = new();

    public IList<ApplicationUser> TaCandidates { get; set; } = new List<ApplicationUser>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var classGroup = await _classes.GetByIdAsync(id);
        if (classGroup is null)
        {
            return NotFound();
        }

        if (classGroup.Course is null || classGroup.Course.InstructorId != userId)
        {
            return Forbid();
        }

        ClassGroup = classGroup;
        Assignments = await _assignments.GetForClassAsync(id);
        TaCandidates = await _userManager.GetUsersInRoleAsync(Roles.TeachingAssistant);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, string userId, ClassAssignmentRole role)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _assignments.AssignAsync(id, ownerId, userId, role);
        TempData["Message"] = ok ? "已分配。" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id, int assignmentId)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _assignments.RevokeAsync(assignmentId, ownerId);
        TempData["Message"] = ok ? "已移除。" : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id, bool close)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ok = await _classes.SetStatusAsync(id, ownerId, close ? ClassGroupStatus.Closed : ClassGroupStatus.Upcoming);
        string resultMessage;
        if (ok)
        {
            resultMessage = close ? "班级已关闭。" : "班级已重新开放。";
        }
        else
        {
            resultMessage = "操作失败。";
        }

        TempData["Message"] = resultMessage;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAnnounceAsync(int id, string title, string body)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            TempData["Message"] = "标题与内容不能为空。";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        await _notifications.SendClassAnnouncementAsync(id, title.Trim(), body.Trim(), senderId);
        TempData["Message"] = "通知已发送给该班级学员。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { id });
    }
}
