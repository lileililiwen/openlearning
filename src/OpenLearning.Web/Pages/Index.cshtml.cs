using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;

namespace OpenLearning.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;

    public IndexModel(CourseService courses, EnrollmentService enrollments)
    {
        _courses = courses;
        _enrollments = enrollments;
    }

    public CourseSearchResult? Results { get; set; }

    public List<string> Categories { get; set; } = new();

    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 9;

    public int TotalPages => Results is null ? 0 : (int)Math.Ceiling(Results.TotalCount / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; }

    public string ActiveSort => string.IsNullOrEmpty(Sort) ? "newest" : Sort;

    public async Task<IActionResult> OnGetAsync(int? page)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToPage("/Admin/Index");
            }

            if (User.IsInRole(Roles.Instructor))
            {
                return RedirectToPage("/Dashboard/Teacher");
            }

            if (User.IsInRole(Roles.Student))
            {
                return RedirectToPage("/Dashboard/Index");
            }
        }

        var sortKey = Sort?.ToLowerInvariant() switch
        {
            "popular" => CourseSort.Popular,
            "price-asc" => CourseSort.PriceAsc,
            "price-desc" => CourseSort.PriceDesc,
            "rating" => CourseSort.Rating,
            _ => CourseSort.Newest,
        };
        CurrentPage = Math.Max(1, page ?? 1);

        Results = await _courses.SearchAsync(Search, Category, sortKey, CurrentPage, PageSize);
        Categories = await _courses.GetCategoriesAsync();

        if (sortKey == CourseSort.Popular && Results.Courses.Count > 0)
        {
            var ids = Results.Courses.Select(c => c.Id).ToList();
            var counts = await _enrollments.GetEnrollmentCountsAsync(ids);
            Results = new CourseSearchResult(
                Results.Courses
                    .OrderByDescending(c => counts.GetValueOrDefault(c.Id))
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList(),
                Results.TotalCount);
        }

        return Page();
    }
}
