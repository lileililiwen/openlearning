using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class CouponsModel : PageModel
{
    private readonly CouponService _coupons;

    public CouponsModel(CouponService coupons)
    {
        _coupons = coupons;
    }

    public List<Coupon> Coupons { get; set; } = new();

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public int? DiscountPercent { get; set; }

    [BindProperty]
    public decimal? DiscountAmount { get; set; }

    [BindProperty]
    public DateTime? ExpiresAt { get; set; }

    [BindProperty]
    public int? MaxUses { get; set; }

    public async Task OnGetAsync()
    {
        Coupons = await _coupons.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (ok, error) = await _coupons.CreateAsync(Code, DiscountPercent, DiscountAmount, ExpiresAt, MaxUses);
        TempData["Message"] = ok ? "Coupon created." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id)
    {
        var (ok, error) = await _coupons.UpdateAsync(id, DiscountPercent, DiscountAmount, ExpiresAt, MaxUses);
        TempData["Message"] = ok ? "Coupon updated." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool active)
    {
        var (ok, error) = await _coupons.SetActiveAsync(id, active);
        string? message;
        if (!ok)
        {
            message = error;
        }
        else if (active)
        {
            message = "Coupon activated.";
        }
        else
        {
            message = "Coupon deactivated.";
        }

        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
