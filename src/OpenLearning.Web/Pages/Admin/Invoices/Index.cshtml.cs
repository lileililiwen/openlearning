using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Invoicing.Models;
using InvoicingInvoiceService = OpenLearning.Invoicing.Services.InvoiceService;

namespace OpenLearning.Web.Pages.Admin.Invoices;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
[OpenLearning.Navigation.Models.Breadcrumb("首页:/", "后台:/Admin/Index", "发票管理")]
public class IndexModel : PageModel
{
    private readonly InvoicingInvoiceService _invoices;

    public IndexModel(InvoicingInvoiceService invoices)
    {
        _invoices = invoices;
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

        TempData["Message"] = ok ? "Invoice issued." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.RejectAsync(id, reviewerId, reason ?? string.Empty);

        TempData["Message"] = ok ? "Request rejected." : error;
        TempData["MessageType"] = ok ? "success" : "danger";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVoidAsync(int id, string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await _invoices.VoidAsync(id, reviewerId, reason ?? string.Empty);

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
}
