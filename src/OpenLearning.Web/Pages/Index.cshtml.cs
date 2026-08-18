using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Ratings.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly ReviewService _reviews;
    private readonly SystemConfigService _config;

    public IndexModel(
        CourseService courses,
        EnrollmentService enrollments,
        ReviewService reviews,
        SystemConfigService config)
    {
        _courses = courses;
        _enrollments = enrollments;
        _reviews = reviews;
        _config = config;
    }

    public CourseSearchResult? Results { get; set; }

    public List<string> Categories { get; set; } = new();

    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 9;

    public int TotalPages => Results is null ? 0 : (int)Math.Ceiling(Results.TotalCount / (double)PageSize);

    /// <summary>Per-course rating aggregate (CourseId → Average+Count). Empty entry for courses with no reviews.</summary>
    public Dictionary<int, RatingAggregate> Ratings { get; set; } = new();

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
        PageSize = Math.Clamp(await _config.GetIntAsync("Catalog.PageSize", 9), 1, 50);

        Results = await _courses.SearchAsync(Search, Category, sortKey, CurrentPage, PageSize);
        Categories = await _courses.GetCategoriesAsync();

        if (Results.Courses.Count > 0)
        {
            var ids = Results.Courses.Select(c => c.Id).ToList();
            var ratingMap = await _reviews.GetRatingsAsync(ids);
            Ratings = ids.ToDictionary(
                id => id,
                id => ratingMap.TryGetValue(id, out var r) ? r : new RatingAggregate(0d, 0));
        }

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
        else if (sortKey == CourseSort.Rating && Results.Courses.Count > 0)
        {
            Results = new CourseSearchResult(
                Results.Courses
                    .OrderByDescending(c => Ratings.GetValueOrDefault(c.Id)?.Average ?? 0d)
                    .ThenByDescending(c => Ratings.GetValueOrDefault(c.Id)?.Count ?? 0)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList(),
                Results.TotalCount);
        }

        return Page();
    }
}
