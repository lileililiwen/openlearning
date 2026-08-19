using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;

namespace OpenLearning.Ecommerce.Services;

public class InvoiceService
{
    private readonly DbContext _db;

    public InvoiceService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Records a Student's invoice request for a paid order.</summary>
    public async Task<(bool Ok, string? Error)> RequestAsync(int orderId, string userId)
    {
        var order = await _db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == userId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return (false, "Only paid orders can have an invoice.");
        }

        if (order.InvoiceRequestedAt is not null)
        {
            return (false, "An invoice was already requested for this order.");
        }

        order.InvoiceRequestedAt = DateTime.UtcNow;
        _db.Set<InvoiceRequest>().Add(new InvoiceRequest { OrderId = orderId, RequestedBy = userId });
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
