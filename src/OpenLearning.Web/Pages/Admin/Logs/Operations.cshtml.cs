using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Logging.Models;
using OpenLearning.Logging.Services;

namespace OpenLearning.Web.Pages.Admin.Logs;

[Authorize(Policy = Policies.RequireAdmin)]
public class OperationsModel : PageModel
{
    private readonly LogService _logs;

    public OperationsModel(LogService logs)
    {
        _logs = logs;
    }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Actor { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<OperationLog> Items { get; set; } = new();

    public int Total { get; set; }

    public static int PageSize => 50;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public async Task OnGetAsync()
    {
        (Items, Total) = await _logs.GetOperationsAsync(Action, Actor, From, To, Math.Max(1, PageNumber), PageSize);
    }
}
