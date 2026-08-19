using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth;

namespace OpenLearning.Web.Pages.Admin.QuestionBank;

[Authorize(Policy = Policies.RequireAdmin)]
public class IndexModel : PageModel
{
    private readonly QuestionBankService _bank;

    public IndexModel(QuestionBankService bank)
    {
        _bank = bank;
    }

    public List<Question> Items { get; set; } = new();

    public int Total { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Topic { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Text { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var (items, total) = await _bank.SearchAsync(Topic, Text, Math.Max(1, PageNumber), 20);
        Items = items;
        Total = total;
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        await _bank.ArchiveAsync(id);
        TempData["Message"] = "题目已归档。";
        return RedirectToPage(new { Topic, Text, PageNumber });
    }
}
