using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Invoicing.Models;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using InvoicingInvoiceService = OpenLearning.Invoicing.Services.InvoiceService;

namespace OpenLearning.Web.Pages.Admin.Invoices;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "发票管理")]
public class IndexModel : PageModel
{
    private readonly InvoicingInvoiceService _invoices;
    private readonly OrderService _orders;
    private readonly NotificationService _notifications;

    public IndexModel(InvoicingInvoiceService invoices, OrderService orders, NotificationService notifications)
    {
        _invoices = invoices;
        _orders = orders;
        _notifications = notifications;
    }

    public List<InvoiceRequest> Requests { get; set; } = new();

    public List<Invoice> Issued { get; set; } = new();

    public async Task OnGetAsync()
    {
        Requests = await _invoices.GetPendingAsync();
        Issued = await _invoices.GetIssuedAsync();
    }

    public async Task<IActionResult> OnPostIssueAsync(int id)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.IssueAsync(id, reviewerId);
        if (ok)
        {
            var request = await _invoices.GetRequestByIdAsync(id);
            if (request?.InvoiceId is int invoiceId)
            {
                await _notifications.CreateAsync(
                    request.StudentUserId,
                    NotificationType.Order,
                    "Invoice issued",
                    $"Your invoice for order #{request.OrderId} is ready.",
                    $"/Invoices/View?id={invoiceId}");
            }
        }

        TempData["Message"] = ok ? "Invoice issued." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.RejectAsync(id, reviewerId, reason ?? string.Empty);
        if (ok)
        {
            var request = await _invoices.GetRequestByIdAsync(id);
            if (request is not null)
            {
                await _notifications.CreateAsync(
                    request.StudentUserId,
                    NotificationType.Order,
                    "Invoice request rejected",
                    $"Your invoice request for order #{request.OrderId} was rejected. Reason: {request.Reason}",
                    $"/Orders/Detail?id={request.OrderId}");
            }
        }

        TempData["Message"] = ok ? "Request rejected." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVoidAsync(int id, string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.VoidAsync(id, reviewerId, reason ?? string.Empty);
        if (ok)
        {
            var orderId = await _invoices.GetOrderIdAsync(id);
            if (orderId is int oid)
            {
                await _notifications.CreateAsync(
                    await GetOrderStudentAsync(oid),
                    NotificationType.Order,
                    "Invoice voided",
                    $"Invoice #{id} was voided. Reason: {reason}",
                    $"/Invoices/View?id={id}");
            }
        }

        TempData["Message"] = ok ? "Invoice voided." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRedLetterAsync(int id)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.IssueRedLetterAsync(id, reviewerId);
        TempData["Message"] = ok ? "Red-letter invoice issued." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    private async Task<string> GetOrderStudentAsync(int orderId)
    {
        var order = await _orders.GetByIdForAdminAsync(orderId);
        return order?.StudentId ?? string.Empty;
    }
}
