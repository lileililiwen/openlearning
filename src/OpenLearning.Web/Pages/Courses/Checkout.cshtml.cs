using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Distribution.Services;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Web.Pages.Courses;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly OrderService _orders;
    private readonly SettlementService _settlement;
    private readonly DistributionService _distribution;

    public CheckoutModel(OrderService orders, SettlementService settlement, DistributionService distribution)
    {
        _orders = orders;
        _settlement = settlement;
        _distribution = distribution;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var order = await _orders.GetPendingOrderAsync(userId, courseId);
        if (order is null)
        {
            var (created, error) = await _orders.CreateAsync(userId, courseId);
            if (error is not null)
            {
                TempData["Message"] = error;
                TempData["MessageType"] = "danger";
                return RedirectToPage("/Courses/Details", new { id = courseId });
            }

            // Reload so the Course navigation is populated for the view.
            order = await _orders.GetPendingOrderAsync(userId, courseId) ?? created;
        }

        Order = order;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _orders.GetByIdAsync(orderId, userId);
        if (order is null)
        {
            return NotFound();
        }

        var (ok, error) = await _orders.ConfirmPaymentAsync(orderId, userId);
        if (ok)
        {
            // Credit the course instructor for the paid order (Web composition).
            if (order.Course?.InstructorId is { Length: > 0 } instructorId)
            {
                await _settlement.CreditAsync(instructorId, order.CourseId, order.Amount, $"Order #{orderId}");
            }

            // Attribute the order to a distributor if it arrived via a share link.
            await _distribution.RecordPaidAsync(orderId, Request.Cookies["ol_aff"]);
        }
        else
        {
            TempData["Message"] = error;
            TempData["MessageType"] = "danger";
        }

        return RedirectToPage("/Courses/Details", new { id = order.CourseId });
    }
}
