using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth.Services;

namespace OpenLearning.Web.Pages.Auth;

public class PhoneLoginModel : PageModel
{
    private readonly PhoneCodeService _codes;
    private readonly IWebHostEnvironment _env;

    public PhoneLoginModel(PhoneCodeService codes, IWebHostEnvironment env)
    {
        _codes = codes;
        _env = env;
    }

    public class InputModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (ok, error, code) = await _codes.IssueAsync(Input.PhoneNumber);
        if (!ok)
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
            return Page();
        }

        var phone = PhoneCodeService.Normalize(Input.PhoneNumber);
        if (_env.IsDevelopment() && code is not null)
        {
            // Dev-only fallback: no SMS gateway, so surface the code on the
            // next page. Never shown in production.
            TempData["DevCode"] = code;
        }

        return RedirectToPage("/Auth/VerifyCode", new { phoneNumber = phone });
    }
}
