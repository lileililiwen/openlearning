using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.UserManagement.Models;
using OpenLearning.UserManagement.Services;

namespace OpenLearning.Web.Pages;

[Authorize]
public class ApplyInstructorModel : PageModel
{
    private readonly InstructorApplicationService _applications;

    public ApplyInstructorModel(InstructorApplicationService applications)
    {
        _applications = applications;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public InstructorApplication? Application { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Please tell us why you want to teach.")]
        [StringLength(2000)]
        [Display(Name = "Why do you want to become an instructor?")]
        public string Motivation { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Application = await _applications.GetForUserAsync(userId);
        if (Application is not null)
        {
            Input.Motivation = Application.Motivation;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _applications.SubmitAsync(userId, Input.Motivation);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not submit your application.");
            await OnGetAsync();
            return Page();
        }

        TempData["Message"] = "Your application has been submitted.";
        TempData["MessageType"] = "success";
        return RedirectToPage();
    }
}
