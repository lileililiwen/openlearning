using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Invoicing.Models;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using InvoiceRequestEntity = OpenLearning.Invoicing.Models.InvoiceRequest;
using InvoicingInvoiceService = OpenLearning.Invoicing.Services.InvoiceService;

namespace OpenLearning.Web.Pages.Orders;

[Authorize]
public class DetailModel : PageModel
{
    private readonly OrderService _orders;
    private readonly InvoicingInvoiceService _invoices;
    private readonly NotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailModel(
        OrderService orders,
        InvoicingInvoiceService invoices,
        NotificationService notifications,
        UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _invoices = invoices;
        _notifications = notifications;
        _userManager = userManager;
    }

    public Order? Order { get; set; }

    public InvoiceRequestEntity? InvoiceRequest { get; set; }

    public List<Invoice> Invoices { get; set; } = new();

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string? TaxId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Order = await _orders.GetByIdAsync(id, userId);
        if (Order is null)
        {
            return NotFound();
        }

        InvoiceRequest = await _invoices.GetRequestForOrderAsync(id, userId);
        Invoices = await _invoices.GetForOrderAsync(id);
        return Page();
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
        if (string.IsNullOrWhiteSpace(Title))
        {
            TempData["Message"] = "A title for the invoice is required.";
            TempData["MessageType"] = "danger";
            return RedirectToPage(new { id });
        }

        var (ok, error) = await _invoices.SubmitAsync(userId, id, Title, TaxId);
        TempData["Message"] = ok ? "Invoice requested. Finance will review it." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}
