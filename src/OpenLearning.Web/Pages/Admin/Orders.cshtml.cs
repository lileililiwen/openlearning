using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class OrdersModel : PageModel
{
    private readonly OrderService _orders;

    public OrdersModel(OrderService orders)
    {
        _orders = orders;
    }

    public const int PageSize = 20;

    [BindProperty(SupportsGet = true)]
    public OrderStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public List<Order> Orders { get; set; } = new();

    public int TotalCount { get; set; }

    public decimal TotalAmount { get; set; }

    public int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public async Task OnGetAsync()
    {
        var (orders, totalCount, totalAmount) = await _orders.GetAdminOrdersAsync(
            new OrderService.OrderFilter(Status, From, To, Search), PageNumber, PageSize);
        Orders = orders;
        TotalCount = totalCount;
        TotalAmount = totalAmount;
    }
}
