using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Logging.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class CategoriesModel : PageModel
{
    private readonly CategoryService _categories;
    private readonly LogService _logs;

    public CategoriesModel(CategoryService categories, LogService logs)
    {
        _categories = categories;
        _logs = logs;
    }

    public List<Category> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _categories.GetAllAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name)
    {
        var (ok, error) = await _categories.CreateAsync(name);
        await LogAsync(ok, ok ? "CreateCategory" : "CreateCategoryFailed", name, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenameAsync(int id, string name)
    {
        var (ok, error) = await _categories.RenameAsync(id, name);
        await LogAsync(ok, ok ? "RenameCategory" : "RenameCategoryFailed", name, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var category = await _categories.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var (ok, error) = await _categories.SetActiveAsync(id, !category.IsActive);
        await LogAsync(ok, ok ? "ToggleCategory" : "ToggleCategoryFailed", category.Name, error);
        Flash(ok, error);
        return RedirectToPage();
    }

    private void Flash(bool ok, string? error)
    {
        TempData["Message"] = ok ? "Saved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
    }

    private async Task LogAsync(bool ok, string action, string target, string? error)
    {
        await _logs.RecordAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.Identity?.Name ?? string.Empty,
            ok ? action : "AdminOperationFailed",
            "Category",
            ok ? target : string.Empty,
            ok ? null : error,
            HttpContext.Connection.RemoteIpAddress?.ToString());
    }
}
