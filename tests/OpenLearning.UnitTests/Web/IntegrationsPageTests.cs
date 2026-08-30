using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenLearning.Auth;
using OpenLearning.Data;
using OpenLearning.Integrations.Contracts;
using OpenLearning.Integrations.Models;
using OpenLearning.Integrations.Services;
using OpenLearning.Web.Pages.Admin.Integrations;
using Xunit;

namespace OpenLearning.UnitTests.Web;

public sealed class IntegrationsPageTests
{
    private static ApplicationDbContext NewDb()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static ClaimsPrincipal User(string nameIdentifier, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, nameIdentifier) };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static IntegrationsModel CreatePage(ClaimsPrincipal user, IntegrationOperationsService service)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        return new IntegrationsModel(service)
        {
            PageContext = new PageContext(actionContext) { ViewData = viewData },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = new UrlHelper(actionContext),
        };
    }

    private static IntegrationOperationsService AdminService(ApplicationDbContext db)
    {
        return new IntegrationOperationsService(db, new[] { new SuccessAdapter() });
    }

    [Fact]
    public async Task OnGetAsync_Admin_CanViewPage()
    {
        await using var db = NewDb();
        var page = CreatePage(User("admin-1", Roles.Admin), AdminService(db));

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NonAdmin_IsForbidden()
    {
        await using var db = NewDb();
        var page = CreatePage(User("student-1", Roles.Student), AdminService(db));

        var result = await page.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostDeclareAsync_Admin_CreatesRegistration()
    {
        await using var db = NewDb();
        var page = CreatePage(User("admin-1", Roles.Admin), AdminService(db));

        var result = await page.OnPostDeclareAsync("Grade webhook", "Webhook", "https://example.com/hook", 1, "deepLinking");

        Assert.IsType<RedirectToPageResult>(result);
        var stored = await db.Set<IntegrationRegistration>().AsNoTracking().SingleAsync();
        Assert.Equal("Grade webhook", stored.Name);
        Assert.Equal("platform", stored.TenantId);
    }

    [Fact]
    public async Task OnPostReplayAsync_Admin_ReplaysDeadLetter()
    {
        await using var db = NewDb();
        var registration = new IntegrationRegistration
        {
            Name = "wh",
            Type = "Webhook",
            ContractVersion = 1,
            TenantId = "platform",
            IsEnabled = true,
            EndpointReference = "https://example.com/hook",
        };
        db.Set<IntegrationRegistration>().Add(registration);
        await db.SaveChangesAsync();

        var dead = new IntegrationDelivery
        {
            RegistrationId = registration.Id,
            IdempotencyKey = "replay-key",
            EventType = "grade.posted",
            PayloadReference = "{}",
            Status = IntegrationDeliveryStatus.DeadLettered,
            TenantId = "platform",
            Attempts = 3,
        };
        db.Set<IntegrationDelivery>().Add(dead);
        await db.SaveChangesAsync();

        var page = CreatePage(User("admin-1", Roles.Admin), AdminService(db));
        var result = await page.OnPostReplayAsync(dead.Id);

        Assert.IsType<RedirectToPageResult>(result);
        var deliveries = await db.Set<IntegrationDelivery>().AsNoTracking().OrderBy(d => d.Id).ToListAsync();
        Assert.Equal(2, deliveries.Count);
        Assert.Equal("replay-key", deliveries[1].IdempotencyKey);
        Assert.Equal(IntegrationDeliveryStatus.Delivered, deliveries[1].Status);
    }

    private sealed class SuccessAdapter : IIntegrationAdapter
    {
        public bool SupportsType(string integrationType)
        {
            return true;
        }

        public Task<IntegrationResult> ValidateAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IntegrationResult.Success());
        }

        public Task<IntegrationResult> ExecuteAsync(IntegrationRegistration registration, IntegrationDeliveryRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IntegrationResult.Success());
        }

        public Task<IntegrationResult> HealthAsync(IntegrationRegistration registration, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IntegrationResult.Success());
        }
    }
}
