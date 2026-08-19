using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Web.Pages.Orders;

[Authorize]
public class DetailModel : PageModel
{
    private readonly OrderService _orders;
    private readonly InvoiceService _invoices;
    private readonly NotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailModel(
        OrderService orders,
        InvoiceService invoices,
        NotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _invoices = invoices;
        _notifications = notifications;
        _userManager = userManager;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Order = await _orders.GetByIdAsync(id, userId);
        return Order is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostRefundAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _orders.RequestRefundAsync(id, userId);
        if (ok)
        {
            var order = await _orders.GetByIdAsync(id, userId);
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            await _notifications.CreateForManyAsync(
                admins.Select(a => a.Id),
                NotificationType.Order,
                $"Refund requested — order #{id}",
                $"{order?.Course?.Title ?? "Course"} order #{id} refund requested by a student.",
                "/Admin/Index");
        }

        TempData["Message"] = ok ? "Refund requested. An admin will review it." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostInvoiceAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.RequestAsync(id, userId);
        TempData["Message"] = ok ? "Invoice requested." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
