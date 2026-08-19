using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Ecommerce.Services;

namespace OpenLearning.Web.Pages.Orders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly OrderService _orders;

    public IndexModel(OrderService orders)
    {
        _orders = orders;
    }

    public List<Order> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Orders = await _orders.GetOrdersForStudentAsync(userId);
    }
}
