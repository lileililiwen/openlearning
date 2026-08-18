using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Auth;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly AccountService _account;

    public LogoutModel(AccountService account)
    {
        _account = account;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _account.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
