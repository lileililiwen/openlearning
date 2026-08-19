using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Services;
using OpenLearning.Web.Pages.Admin.Reports;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireFinanceOrAdmin)]
public class ReconciliationModel : PageModel
{
    private readonly OrderService _orders;

    public ReconciliationModel(OrderService orders)
    {
        _orders = orders;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public List<OrderService.ReconRow> Rows { get; set; } = new();

    public int TotalGrossOrders { get; set; }

    public decimal TotalGross { get; set; }

    public int TotalRefundedOrders { get; set; }

    public decimal TotalRefunds { get; set; }

    public decimal TotalNet { get; set; }

    public async Task OnGetAsync()
    {
        var (rows, grossOrders, gross, refundedOrders, refunds, net) =
            await _orders.GetReconciliationAsync(From, To);
        Rows = rows;
        TotalGrossOrders = grossOrders;
        TotalGross = gross;
        TotalRefundedOrders = refundedOrders;
        TotalRefunds = refunds;
        TotalNet = net;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var (rows, _, _, _, _, _) = await _orders.GetReconciliationAsync(From, To);
        var header = new[] { "Course", "GrossOrders", "Gross", "RefundedOrders", "Refunds", "Net" };
        var data = rows.Select(r => new string?[]
        {
            r.CourseTitle,
            r.GrossOrders.ToString(CultureInfo.InvariantCulture),
            r.Gross.ToString("0.00", CultureInfo.InvariantCulture),
            r.RefundedOrders.ToString(CultureInfo.InvariantCulture),
            r.Refunds.ToString("0.00", CultureInfo.InvariantCulture),
            r.Net.ToString("0.00", CultureInfo.InvariantCulture),
        });
        var csv = CsvHelper.Build(header, data);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "reconciliation.csv");
    }
}
