using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize(Policy = Policies.RequireInstructor)]
public class CreateModel : PageModel
{
    private readonly CourseService _courses;

    public CreateModel(CourseService courses)
    {
        _courses = courses;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        [Range(0, 99999, ErrorMessage = "Price must be between 0 and 99999.")]
        [Display(Name = "Price (leave blank for free)")]
        public decimal? Price { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var course = await _courses.CreateAsync(userId, Input.Title, Input.Description, Input.Category, Input.Price);
        return RedirectToPage("/Courses/Edit", new { id = course!.Id });
    }
}
