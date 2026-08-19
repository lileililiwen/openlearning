using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Invoicing.Models;
using InvoicingInvoiceService = OpenLearning.Invoicing.Services.InvoiceService;

namespace OpenLearning.Web.Pages.Invoices;

public class ViewModel : PageModel
{
    private readonly InvoicingInvoiceService _invoices;
    private readonly OrderService _orders;

    public ViewModel(InvoicingInvoiceService invoices, OrderService orders)
    {
        _invoices = invoices;
        _orders = orders;
    }

    public Invoice? Invoice { get; set; }

    public Order? Order { get; set; }

    public Invoice? Original { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var invoice = await _invoices.GetByIdAsync(id);
        if (invoice is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isFinance = User.IsInRole(Roles.Finance) || User.IsInRole(Roles.Admin);
        var order = isFinance
            ? await _orders.GetByIdForAdminAsync(invoice.OrderId)
            : await _orders.GetByIdAsync(invoice.OrderId, userId);
        if (order is null)
        {
            return Forbid();
        }

        Invoice = invoice;
        Order = order;
        if (invoice.OriginalInvoiceId is int originalId)
        {
            Original = await _invoices.GetByIdAsync(originalId);
        }

        return Page();
    }
}
