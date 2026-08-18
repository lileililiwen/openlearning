using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Admin.Reports;

[Authorize(Policy = Policies.RequireAdmin)]
public class RevenueModel : PageModel
{
    private readonly OrderService _orders;

    public RevenueModel(OrderService orders)
    {
        _orders = orders;
    }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public List<OrderService.RevenueByCourseRow> Rows { get; set; } = new();

    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    private static readonly string[] _csvHeaders = new[] { "Id", "PaidAt", "Course", "Student", "Amount", "Status", "Reference" };

    public async Task OnGetAsync()
    {
        var (rows, totalRevenue, totalOrders) = await _orders.GetRevenueReportAsync(From, To);
        Rows = rows;
        TotalRevenue = totalRevenue;
        TotalOrders = totalOrders;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var orders = await _orders.GetPaidOrdersForExportAsync(From, To);
        var rows = orders.Select(o => new string?[]
        {
            o.Id.ToString(CultureInfo.InvariantCulture),
            (o.PaidAt ?? o.CreatedAt).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            o.Course?.Title ?? string.Empty,
            o.Student?.DisplayName ?? string.Empty,
            o.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            o.Status.ToString(),
            o.PaymentReference,
        });
        var csv = CsvHelper.Build(
            _csvHeaders,
            rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "orders.csv");
    }
}
