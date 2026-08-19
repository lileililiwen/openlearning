using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Distribution.Models;
using OpenLearning.Distribution.Services;

namespace OpenLearning.Web.Pages.Distributor;

[Authorize(Policy = Policies.RequireDistributor)]
public class LinksModel : PageModel
{
    private readonly DistributionService _distribution;
    private readonly CourseService _courses;

    public LinksModel(DistributionService distribution, CourseService courses)
    {
        _distribution = distribution;
        _courses = courses;
    }

    public List<AffiliateLink> Links { get; set; } = new();

    public List<Course> Courses { get; set; } = new();

    [BindProperty]
    public int CourseId { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Links = await _distribution.GetLinksAsync(userId);
        Courses = await _courses.GetPublishedCoursesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (link, error) = await _distribution.CreateLinkAsync(userId, CourseId);
        TempData["Message"] = error ?? (link is not null ? $"Share link created: /D/C/{link.Slug}" : "Created.");
        TempData["MessageType"] = error is null ? "success" : "danger";
        return RedirectToPage();
    }
}
