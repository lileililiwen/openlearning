using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Certificates.Models;
using OpenLearning.Certificates.Services;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Certificates;

[Authorize]
public class ViewModel : PageModel
{
    private readonly CertificateService _certificates;
    private readonly CourseService _courses;

    public ViewModel(CertificateService certificates, CourseService courses)
    {
        _certificates = certificates;
        _courses = courses;
    }

    public Certificate? Certificate { get; set; }

    public string? RecipientName { get; set; }

    public string? CourseTitle { get; set; }

    public string? InstructorName { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var certificate = await _certificates.GetByIdAsync(id);
        if (certificate is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isOwner = await _courses.IsOwnerAsync(certificate.CourseId, userId);
        var isAdmin = User.IsInRole(Roles.Admin);
        if (certificate.UserId != userId && !isOwner && !isAdmin)
        {
            return Forbid();
        }

        Certificate = certificate;
        RecipientName = certificate.User?.DisplayName ?? string.Empty;
        CourseTitle = certificate.Course?.Title ?? string.Empty;
        InstructorName = certificate.Course?.Instructor?.DisplayName ?? string.Empty;
        return Page();
    }
}
