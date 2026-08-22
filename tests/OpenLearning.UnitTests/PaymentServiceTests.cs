using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Payments.Models;
using OpenLearning.Payments.Services;
using Xunit;

namespace OpenLearning.UnitTests.Payments;

public sealed class PaymentServiceTests
{
    private const string _secret = "test-secret";
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private static async Task<(ApplicationDbContext Db, PaymentService Service, Order Order)> SetupAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var course = new Course { Title = "Gateway course", InstructorId = "teacher", Status = CourseStatus.Published, Price = 88m };
        db.Add(course);
        await db.SaveChangesAsync();
        var order = new Order { CourseId = course.Id, StudentId = "student", Amount = 88m };
        db.Add(order);
        await db.SaveChangesAsync();
        var service = new PaymentService(db, new DeterministicFakePaymentProvider(_secret), new EnrollmentService(db));
        return (db, service, order);
    }

    [Fact]
    public async Task Invalid_signature_and_amount_mismatch_never_fulfill()
    {
        var (db, service, order) = await SetupAsync();
        var created = await service.CreateAsync(order.Id, order.StudentId);
        var body = Body("evt-1", created.Intent!.ProviderIntentId, 99m);
        Assert.False((await service.IngestAsync(body, "invalid")).Ok);
        Assert.False((await service.IngestAsync(body, Sign(body))).Ok);
        Assert.Equal(OrderStatus.Pending, (await db.Set<Order>().FindAsync(order.Id))!.Status);
        Assert.Single(db.Set<PaymentReconciliationIssue>());
    }

    [Fact]
    public async Task Verified_duplicate_success_fulfills_exactly_once()
    {
        var (db, service, order) = await SetupAsync();
        var created = await service.CreateAsync(order.Id, order.StudentId);
        var body = Body("evt-success", created.Intent!.ProviderIntentId, order.Amount);
        Assert.True((await service.IngestAsync(body, Sign(body))).Ok);
        var duplicate = await service.IngestAsync(body, Sign(body));
        Assert.True(duplicate.Ok);
        Assert.True(duplicate.Duplicate);
        Assert.Equal(OrderStatus.Paid, (await db.Set<Order>().FindAsync(order.Id))!.Status);
        Assert.Single(db.Set<OpenLearning.Enrollment.Models.Enrollment>());
        Assert.Single(db.Set<ProviderEvent>());
        Assert.Single(db.Set<PaymentOutbox>());
    }

    [Fact]
    public async Task Refund_cannot_exceed_remaining_amount()
    {
        var (_, service, order) = await SetupAsync();
        var created = await service.CreateAsync(order.Id, order.StudentId);
        var body = Body("evt-paid", created.Intent!.ProviderIntentId, order.Amount);
        await service.IngestAsync(body, Sign(body));
        Assert.NotNull((await service.RequestRefundAsync(created.Intent.Id, 50m, "admin")).Refund);
        Assert.NotNull((await service.RequestRefundAsync(created.Intent.Id, 39m, "admin")).Error);
    }

    private static byte[] Body(string eventId, string intentId, decimal amount)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            new VerifiedPaymentEvent(eventId, intentId, "payment.succeeded", amount, "CNY"), _jsonOptions);
    }

    private static string Sign(byte[] body)
    {
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_secret), body)).ToLowerInvariant();
    }
}
