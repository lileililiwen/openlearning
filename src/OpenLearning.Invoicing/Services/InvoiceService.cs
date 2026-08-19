using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Invoicing.Models;
using OpenLearning.SystemConfig.Services;
using InvoiceRequestEntity = OpenLearning.Invoicing.Models.InvoiceRequest;

namespace OpenLearning.Invoicing.Services;

/// <summary>Allocates the next sequential invoice number from system-config.</summary>
public class InvoiceNumberService
{
    public const string NextNumberKey = "invoice.nextNumber";

    private readonly SystemConfigService _config;

    public InvoiceNumberService(SystemConfigService config)
    {
        _config = config;
    }

    /// <summary>Returns the next number and advances the counter. Sequential in normal flow.</summary>
    public async Task<int> AllocateNextAsync()
    {
        var next = await _config.GetIntAsync(NextNumberKey, 100000);
        await _config.SetManyAsync(new Dictionary<string, string>
        {
            [NextNumberKey] = (next + 1).ToString(CultureInfo.InvariantCulture),
        });
        return next;
    }

    /// <summary>Formats a number with the configured prefix and padding width.</summary>
    public async Task<string> FormatAsync(int number)
    {
        var prefix = await _config.GetStringAsync("invoice.prefix", string.Empty);
        var padding = Math.Clamp(await _config.GetIntAsync("invoice.padding", 6), 1, 20);
        return prefix + number.ToString("D" + padding, CultureInfo.InvariantCulture);
    }
}

/// <summary>Invoice request lifecycle and invoice issuing.</summary>
public class InvoiceService
{
    private readonly DbContext _db;
    private readonly InvoiceNumberService _numbers;

    public InvoiceService(DbContext db, InvoiceNumberService numbers)
    {
        _db = db;
        _numbers = numbers;
    }

    public async Task<(bool Ok, string? Error)> SubmitAsync(string studentId, int orderId, string title, string? taxId)
    {
        var order = await _db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StudentId == studentId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return (false, "Invoices can only be requested for paid orders.");
        }

        var existing = await _db.Set<InvoiceRequestEntity>()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.Status == InvoiceRequestStatus.Requested);
        if (existing is not null)
        {
            return (false, "An invoice request for this order is already pending review.");
        }

        _db.Set<InvoiceRequestEntity>().Add(new InvoiceRequestEntity
        {
            OrderId = orderId,
            StudentUserId = studentId,
            Title = title.Trim(),
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<InvoiceRequestEntity>> GetPendingAsync()
    {
        return _db.Set<InvoiceRequestEntity>().AsNoTracking()
            .Where(r => r.Status == InvoiceRequestStatus.Requested)
            .Include(r => r.Student)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public Task<InvoiceRequestEntity?> GetRequestByIdAsync(int id)
    {
        return _db.Set<InvoiceRequestEntity>().AsNoTracking()
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task<InvoiceRequestEntity?> GetRequestForOrderAsync(int orderId, string studentId)
    {
        return _db.Set<InvoiceRequestEntity>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.StudentUserId == studentId);
    }

    public async Task<(bool Ok, string? Error)> IssueAsync(int requestId, string reviewerId)
    {
        var request = await _db.Set<InvoiceRequestEntity>()
            .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == InvoiceRequestStatus.Requested);
        if (request is null)
        {
            return (false, "Request not found or already reviewed.");
        }

        var order = await _db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order is null || order.Status != OrderStatus.Paid)
        {
            return (false, "The order is no longer paid.");
        }

        var number = await _numbers.AllocateNextAsync();
        var invoice = new Invoice
        {
            Number = await _numbers.FormatAsync(number),
            OrderId = order.Id,
            Amount = order.Amount,
            IssuedBy = reviewerId,
        };
        _db.Set<Invoice>().Add(invoice);
        await _db.SaveChangesAsync();

        request.Status = InvoiceRequestStatus.Issued;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = reviewerId;
        request.InvoiceId = invoice.Id;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RejectAsync(int requestId, string reviewerId, string reason)
    {
        var request = await _db.Set<InvoiceRequestEntity>()
            .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == InvoiceRequestStatus.Requested);
        if (request is null)
        {
            return (false, "Request not found or already reviewed.");
        }

        request.Status = InvoiceRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = reviewerId;
        request.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> VoidAsync(int invoiceId, string reviewerId, string reason)
    {
        var invoice = await _db.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return (false, "Invoice not found.");
        }

        if (invoice.VoidedAt is not null)
        {
            return (false, "This invoice is already voided.");
        }

        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> IssueRedLetterAsync(int originalInvoiceId, string reviewerId)
    {
        var original = await _db.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == originalInvoiceId);
        if (original is null)
        {
            return (false, "Original invoice not found.");
        }

        var number = await _numbers.AllocateNextAsync();
        _db.Set<Invoice>().Add(new Invoice
        {
            Number = await _numbers.FormatAsync(number),
            OrderId = original.OrderId,
            Amount = -original.Amount,
            Type = InvoiceType.RedLetter,
            IssuedBy = reviewerId,
            OriginalInvoiceId = original.Id,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<Invoice?> GetByIdAsync(int id)
    {
        return _db.Set<Invoice>().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public Task<List<Invoice>> GetIssuedAsync()
    {
        return _db.Set<Invoice>().AsNoTracking()
            .OrderByDescending(i => i.IssuedAt)
            .Take(100)
            .ToListAsync();
    }

    public Task<List<Invoice>> GetForOrderAsync(int orderId)
    {
        return _db.Set<Invoice>().AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .OrderBy(i => i.IssuedAt)
            .ToListAsync();
    }

    /// <summary>Resolves the order id for an invoice (for ownership checks).</summary>
    public Task<int?> GetOrderIdAsync(int invoiceId)
    {
        return _db.Set<Invoice>().AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => (int?)i.OrderId)
            .FirstOrDefaultAsync();
    }
}
