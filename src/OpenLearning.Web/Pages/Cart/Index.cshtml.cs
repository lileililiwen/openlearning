using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Distribution.Services;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Web.Pages.Cart;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CartService _cart;
    private readonly OrderService _orders;
    private readonly LedgerService _ledger;
    private readonly SettlementService _settlement;
    private readonly DistributionService _distribution;

    public IndexModel(CartService cart, OrderService orders, LedgerService ledger, SettlementService settlement, DistributionService distribution)
    {
        _cart = cart;
        _orders = orders;
        _ledger = ledger;
        _settlement = settlement;
        _distribution = distribution;
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

        // Credit each course's instructor for the paid orders (Web composition —
        // the ecommerce module cannot reference the settlement module).
        if (result.CheckoutId is not null)
        {
            var createdOrders = await _orders.GetOrdersByCheckoutIdAsync(result.CheckoutId.Value);
            foreach (var order in createdOrders)
            {
                if (order.Course?.InstructorId is { Length: > 0 } instructorId)
                {
                    await _settlement.CreditAsync(instructorId, order.CourseId, order.Amount, $"Order #{order.Id}");
                }

                await _distribution.RecordPaidAsync(order.Id, Request.Cookies["ol_aff"]);
            }
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
