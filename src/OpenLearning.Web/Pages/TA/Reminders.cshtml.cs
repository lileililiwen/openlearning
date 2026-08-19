using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.TA;

[Authorize(Policy = Policies.RequireTeachingAssistant)]
public class RemindersModel : PageModel
{
    private readonly IClassAssignmentLookup _lookup;
    private readonly ClassGroupService _classes;
    private readonly NotificationService _notifications;

    public RemindersModel(IClassAssignmentLookup lookup, ClassGroupService classes, NotificationService notifications)
    {
        _lookup = lookup;
        _classes = classes;
        _notifications = notifications;
    }

    public ClassGroup? ClassGroup { get; set; }

    public async Task<IActionResult> OnGetAsync(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        ClassGroup = await _classes.GetByIdAsync(classId);
        if (ClassGroup is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int classId, string title, string body)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _lookup.IsAssignedAsync(userId, classId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            TempData["Message"] = "标题与内容不能为空。";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { classId });
        }

        await _notifications.SendClassAnnouncementAsync(classId, title.Trim(), body.Trim(), userId);
        TempData["Message"] = "提醒已发送给班级学员。";
        TempData["MessageType"] = "success";
        return RedirectToPage(new { classId });
    }
}
