using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Admin.Reports;

[Authorize(Policy = Policies.RequireAdmin)]
public class UsersModel : PageModel
{
    private readonly UserService _users;

    public UsersModel(UserService users)
    {
        _users = users;
    }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public List<(DateTime Day, int Count)> SignupsOverTime { get; set; } = new();

    public List<(string Role, int Count)> SignupsByRole { get; set; } = new();

    public int TotalSignups { get; set; }

    public int Suspended { get; set; }

    public int MaxDayCount => SignupsOverTime.Count == 0 ? 0 : SignupsOverTime.Max(x => x.Count);

    private static readonly string[] _csvHeaders = new[] { "Id", "CreatedAt", "Email", "DisplayName", "Roles", "Suspended" };

    public async Task OnGetAsync()
    {
        SignupsOverTime = await _users.GetSignupsOverTimeAsync(From, To);
        SignupsByRole = await _users.GetSignupsByRoleAsync(From, To);
        TotalSignups = await _users.CountSignupsAsync(From, To);
        Suspended = await _users.CountSuspendedAsync(From, To);
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var users = await _users.GetUsersForExportAsync(From, To);
        var rows = users.Select(u => new string?[]
        {
            u.User.Id,
            u.User.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            u.User.Email,
            u.User.DisplayName,
            u.Roles,
            u.User.IsSuspended ? "Yes" : "No",
        });
        var csv = CsvHelper.Build(
            _csvHeaders,
            rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "users.csv");
    }
}
