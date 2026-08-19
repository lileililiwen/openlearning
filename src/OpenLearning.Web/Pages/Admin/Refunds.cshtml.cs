using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Settlement.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class RefundsModel : PageModel
{
    private readonly OrderService _orders;
    private readonly SettlementService _settlement;
    private readonly NotificationService _notifications;

    public RefundsModel(OrderService orders, SettlementService settlement, NotificationService notifications)
    {
        _orders = orders;
        _settlement = settlement;
        _notifications = notifications;
    }

    public List<Order> Requests { get; set; } = new();

    public async Task OnGetAsync()
    {
        Requests = await _orders.GetRefundRequestsAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var order = await _orders.GetByIdForAdminAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var (ok, error) = await _orders.ReviewRefundAsync(id, approve: true);
        if (ok)
        {
            // Reverse the instructor's earned amount in the settlement ledger.
            if (order.Course?.InstructorId is { Length: > 0 } instructorId)
            {
                await _settlement.CreditAsync(instructorId, order.CourseId, -order.Amount, $"Refund order #{id}");
            }

            await _notifications.CreateAsync(
                order.StudentId,
                NotificationType.Order,
                $"Refund approved — order #{id}",
                $"Your refund for {order.Course?.Title ?? "your course"} was approved.",
                "/Orders/Detail?id=" + id);
        }

        TempData["Message"] = ok ? $"Refund for order #{id} approved." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        var order = await _orders.GetByIdForAdminAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var (ok, error) = await _orders.ReviewRefundAsync(id, approve: false);
        if (ok)
        {
            await _notifications.CreateAsync(
                order.StudentId,
                NotificationType.Order,
                $"Refund rejected — order #{id}",
                $"Your refund request for {order.Course?.Title ?? "your course"} was not approved.",
                "/Orders/Detail?id=" + id);
        }

        TempData["Message"] = ok ? $"Refund for order #{id} rejected." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
