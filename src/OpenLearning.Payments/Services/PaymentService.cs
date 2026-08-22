using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Payments.Models;

namespace OpenLearning.Payments.Services;

public sealed class PaymentService(DbContext db, IPaymentProvider provider, EnrollmentService enrollments)
{
    public sealed record ProviderHealth(string Provider, bool IsAvailable, string Message);

    public Task<PaymentIntent?> GetAsync(Guid id)
    {
        return db.Set<PaymentIntent>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<PaymentIntent>> GetRecentAsync()
    {
        return db.Set<PaymentIntent>().AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
    }

    public Task<List<PaymentReconciliationIssue>> GetOpenIssuesAsync()
    {
        return db.Set<PaymentReconciliationIssue>().AsNoTracking().Where(x => x.State == ReconciliationState.Open).OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<ProviderHealth> GetProviderHealthAsync()
    {
        try
        {
            await provider.GetStateAsync("health-check");
            return new ProviderHealth(provider.Name, true, "Provider adapter is responding.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ProviderHealth(provider.Name, false, "Provider adapter is unavailable.");
        }
    }

    public async Task<(PaymentIntent? Intent, string? RedirectUrl, string? Error)> CreateAsync(int orderId, string studentId)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(x => x.Id == orderId && x.StudentId == studentId);
        if (order is null || order.Status != OrderStatus.Pending)
            return (null, null, "Eligible pending order not found.");
        var existing = await db.Set<PaymentIntent>().FirstOrDefaultAsync(x => x.OrderId == orderId);
        if (existing is not null)
        {
            if (existing.Amount != order.Amount)
                return (null, null, "Order amount changed; start a new checkout.");
            return (existing, $"/Payments/Sandbox?id={existing.Id}", null);
        }
        var intent = new PaymentIntent { OrderId = orderId, Provider = provider.Name, Amount = order.Amount };
        var session = await provider.CreateAsync(intent.Id, intent.Amount, intent.Currency);
        intent.ProviderIntentId = session.IntentId;
        db.Add(intent);
        db.Add(new PaymentAttempt { PaymentIntentId = intent.Id, ProviderReference = session.IntentId });
        await db.SaveChangesAsync();
        return (intent, session.RedirectUrl, null);
    }

    public async Task<(bool Ok, bool Duplicate, string? Error)> IngestAsync(byte[] body, string signature)
    {
        if (!provider.TryVerify(body, signature, out var evt) || evt is null)
            return (false, false, "Invalid signature or payload.");
        if (await db.Set<ProviderEvent>().AnyAsync(x => x.Provider == provider.Name && x.ProviderEventId == evt.EventId))
            return (true, true, null);
        var intent = await db.Set<PaymentIntent>().FirstOrDefaultAsync(x => x.Provider == provider.Name && x.ProviderIntentId == evt.IntentId);
        if (intent is null)
            return (false, false, "Unknown payment intent.");
        db.Add(new ProviderEvent { Provider = provider.Name, ProviderEventId = evt.EventId, EventType = evt.Type, SignatureValid = true, PayloadHash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant() });
        if (evt.Amount != intent.Amount || !string.Equals(evt.Currency, intent.Currency, StringComparison.OrdinalIgnoreCase))
        {
            db.Add(new PaymentReconciliationIssue { PaymentIntentId = intent.Id, Reason = "Provider amount or currency mismatch." });
            await db.SaveChangesAsync();
            return (false, false, "Payment evidence mismatch.");
        }
        if (evt.Type == "payment.succeeded" && intent.State != PaymentState.Succeeded)
        {
            intent.State = PaymentState.Succeeded;
            intent.SucceededAt = DateTime.UtcNow;
            db.Add(new PaymentOutbox { PaymentIntentId = intent.Id });
        }
        await db.SaveChangesAsync();
        await FulfillAsync(intent.Id);
        return (true, false, null);
    }

    public async Task FulfillAsync(Guid intentId)
    {
        var intent = await db.Set<PaymentIntent>().FirstAsync(x => x.Id == intentId);
        var message = await db.Set<PaymentOutbox>().FirstOrDefaultAsync(x => x.PaymentIntentId == intentId && x.ProcessedAt == null);
        if (intent.State != PaymentState.Succeeded || message is null)
            return;
        var order = await db.Set<Order>().FirstAsync(x => x.Id == intent.OrderId);
        if (order.Amount != intent.Amount)
            return;
        if (order.Status != OrderStatus.Paid)
        {
            order.Status = OrderStatus.Paid;
            order.PaidAt = intent.SucceededAt;
            order.PaymentReference = intent.ProviderIntentId;
            await db.SaveChangesAsync();
        }
        if (!await enrollments.IsEnrolledAsync(order.StudentId, order.CourseId))
        {
            var enrolled = await enrollments.EnrollAsync(order.StudentId, order.CourseId);
            if (!enrolled.Ok)
                throw new InvalidOperationException(enrolled.Error);
        }
        message.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<(PaymentRefund? Refund, string? Error)> RequestRefundAsync(Guid intentId, decimal amount, string actor)
    {
        var intent = await db.Set<PaymentIntent>().FirstOrDefaultAsync(x => x.Id == intentId && x.State == PaymentState.Succeeded);
        if (intent is null)
            return (null, "Confirmed payment not found.");
        var refunded = await db.Set<PaymentRefund>().Where(x => x.PaymentIntentId == intentId && x.State != RefundState.Failed).SumAsync(x => (decimal?)x.Amount) ?? 0;
        if (amount <= 0 || amount > intent.Amount - refunded)
            return (null, "Refund exceeds the remaining paid amount.");
        var refund = new PaymentRefund { PaymentIntentId = intentId, Amount = amount, RequestedBy = actor, ProviderRefundId = await provider.RefundAsync(intent.ProviderIntentId, amount) };
        db.Add(refund);
        await db.SaveChangesAsync();
        return (refund, null);
    }

    public async Task ReconcileAsync(Guid intentId)
    {
        var intent = await db.Set<PaymentIntent>().FirstAsync(x => x.Id == intentId);
        try
        { _ = await provider.GetStateAsync(intent.ProviderIntentId); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { db.Add(new PaymentReconciliationIssue { PaymentIntentId = intentId, Reason = "Provider lookup unavailable.", Retryable = true }); await db.SaveChangesAsync(); }
    }
}
