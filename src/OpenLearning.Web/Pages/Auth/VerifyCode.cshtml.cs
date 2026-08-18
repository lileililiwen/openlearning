using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Auth;

public class VerifyCodeModel : PageModel
{
    private readonly PhoneCodeService _codes;
    private readonly AccountService _account;

    public VerifyCodeModel(PhoneCodeService codes, AccountService account)
    {
        _codes = codes;
        _account = account;
    }

    public class InputModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code is 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }

    [BindProperty(SupportsGet = true)]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        PhoneNumber = PhoneCodeService.Normalize(PhoneNumber);
        if (PhoneNumber.Length == 0)
        {
            return RedirectToPage("/Auth/PhoneLogin");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PhoneNumber = PhoneCodeService.Normalize(PhoneNumber);
        if (PhoneNumber.Length == 0)
        {
            return RedirectToPage("/Auth/PhoneLogin");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (ok, error) = await _codes.VerifyAsync(PhoneNumber, Input.Code);
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
            return Page();
        }

        var (signedIn, signInError) = await _account.SignInByPhoneAsync(PhoneNumber);
        if (!signedIn)
        {
            TempData["Message"] = signInError;
            TempData["MessageType"] = "danger";
            return Page();
        }

        return RedirectToPage("/Index");
    }
}
