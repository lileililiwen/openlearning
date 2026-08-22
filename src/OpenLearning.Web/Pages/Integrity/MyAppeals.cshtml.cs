using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Data;
using OpenLearning.Exams.Models;

namespace OpenLearning.Web.Pages.Integrity;

/// <summary>Learner view of their appeal statuses.</summary>
public class MyAppealsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public MyAppealsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<IntegrityAppeal> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return Challenge();
        }

        Items = await _db.IntegrityAppeals
            .Where(a => a.StudentId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return Page();
    }
}
