using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Cart;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CartService _cart;
    private readonly OrderService _orders;
    private readonly LedgerService _ledger;

    public IndexModel(CartService cart, OrderService orders, LedgerService ledger)
    {
        _cart = cart;
        _orders = orders;
        _ledger = ledger;
    }

    public List<CartItem> Items { get; set; } = new();

    public decimal Total { get; set; }

    public decimal Balance { get; set; }

    public int Points { get; set; }

    [BindProperty]
    public string? CouponCode { get; set; }

    [BindProperty]
    public bool UseBalance { get; set; }

    [BindProperty]
    public bool UsePoints { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _cart.RemoveAsync(userId, courseId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _orders.CheckoutCartAsync(userId, CouponCode, UseBalance, UsePoints);
        if (result.Error is not null)
        {
            TempData["Message"] = result.Error;
            TempData["MessageType"] = "danger";
            return RedirectToPage();
        }

        TempData["Message"] =
            $"Checkout complete — {result.OrderCount} course(s) paid, " +
            $"total {result.TotalPaid.ToString("C", CultureInfo.InvariantCulture)} " +
            $"(discount {result.TotalDiscount.ToString("C", CultureInfo.InvariantCulture)}, {result.PointsAwarded} points earned).";
        TempData["MessageType"] = "success";
        return RedirectToPage("/Orders/Index");
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Items = await _cart.GetItemsAsync(userId);
        Total = Items.Sum(i => i.Course?.Price ?? 0m);
        Balance = await _ledger.GetBalanceAsync(userId);
        Points = await _ledger.GetPointsAsync(userId);
    }
}
