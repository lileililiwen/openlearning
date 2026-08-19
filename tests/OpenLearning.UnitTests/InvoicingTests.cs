using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Invoicing.Models;
using OpenLearning.Invoicing.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;
using InvoiceRequestEntity = OpenLearning.Invoicing.Models.InvoiceRequest;

namespace OpenLearning.UnitTests.Invoicing;

public sealed class InvoicingTests
{
    private static (ApplicationDbContext Db, InvoiceService Service, InvoiceNumberService Numbers, Order Order) SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i", Status = CourseStatus.Published, Price = 50m };
        db.Set<Course>().Add(course);
        db.SaveChanges();
        var order = new Order { CourseId = course.Id, StudentId = "s1", Amount = 50m, Status = OrderStatus.Paid };
        db.Set<Order>().Add(order);
        db.SaveChanges();
        var config = new SystemConfigService(db);
        var numbers = new InvoiceNumberService(config);
        return (db, new InvoiceService(db, numbers), numbers, order);
    }

    [Fact]
    public async Task Submit_rejects_unpaid_order_and_duplicates()
    {
        var (db, service, _, _) = SeedAsync();
        var unpaid = new Order { CourseId = 1, StudentId = "s2", Amount = 50m, Status = OrderStatus.Pending };
        db.Set<Order>().Add(unpaid);
        await db.SaveChangesAsync();

        var unpaidResult = await service.SubmitAsync("s2", unpaid.Id, "Invoice", null);
        Assert.False(unpaidResult.Ok);

        var first = await service.SubmitAsync("s1", 1, "Invoice", "TAX-1");
        Assert.True(first.Ok);
        var second = await service.SubmitAsync("s1", 1, "Invoice", "TAX-1");
        Assert.False(second.Ok);
        Assert.Single(db.Set<InvoiceRequestEntity>());
    }

    [Fact]
    public async Task Number_allocation_is_sequential_and_formatted()
    {
        var (_, _, numbers, _) = SeedAsync();
        Assert.Equal(100000, await numbers.AllocateNextAsync());
        Assert.Equal("100000", await numbers.FormatAsync(100000));
        Assert.Equal(100001, await numbers.AllocateNextAsync());
        Assert.Equal("100001", await numbers.FormatAsync(100001));
    }

    [Fact]
    public async Task Issue_allocates_number_and_marks_request_issued()
    {
        var (db, service, _, order) = SeedAsync();
        await service.SubmitAsync("s1", order.Id, "Invoice", null);
        var request = await db.Set<InvoiceRequestEntity>().SingleAsync();
        var (ok, _) = await service.IssueAsync(request.Id, "finance-1");

        Assert.True(ok);
        var invoice = Assert.Single(db.Set<Invoice>());
        Assert.Equal("100000", invoice.Number);
        Assert.Equal(order.Amount, invoice.Amount);
        Assert.Equal("finance-1", invoice.IssuedBy);
        var updated = await db.Set<InvoiceRequestEntity>().FindAsync(request.Id);
        Assert.Equal(InvoiceRequestStatus.Issued, updated!.Status);
        Assert.Equal(invoice.Id, updated.InvoiceId);
    }

    [Fact]
    public async Task Reject_and_void_and_red_letter()
    {
        var (db, service, _, order) = SeedAsync();
        await service.SubmitAsync("s1", order.Id, "Invoice", null);
        var request = await db.Set<InvoiceRequestEntity>().SingleAsync();

        var rejected = await service.RejectAsync(request.Id, "finance-1", "missing tax id");
        Assert.True(rejected.Ok);
        Assert.Equal(InvoiceRequestStatus.Rejected, (await db.Set<InvoiceRequestEntity>().FindAsync(request.Id))!.Status);

        await service.SubmitAsync("s1", order.Id, "Invoice", "TAX-1");
        var request2 = await db.Set<InvoiceRequestEntity>().FirstAsync(r => r.Status == InvoiceRequestStatus.Requested);
        await service.IssueAsync(request2.Id, "finance-1");
        var invoice = await db.Set<Invoice>().SingleAsync();

        Assert.True((await service.VoidAsync(invoice.Id, "finance-1", "duplicate")).Ok);
        Assert.NotNull((await db.Set<Invoice>().FindAsync(invoice.Id))!.VoidedAt);
        Assert.False((await service.VoidAsync(invoice.Id, "finance-1", "again")).Ok);

        Assert.True((await service.IssueRedLetterAsync(invoice.Id, "finance-1")).Ok);
        var red = await db.Set<Invoice>().SingleAsync(i => i.Type == InvoiceType.RedLetter);
        Assert.Equal(-order.Amount, red.Amount);
        Assert.Equal(invoice.Id, red.OriginalInvoiceId);
        Assert.Equal("100001", red.Number);
    }
}
