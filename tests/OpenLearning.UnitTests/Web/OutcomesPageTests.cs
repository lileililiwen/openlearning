using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
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
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Outcomes.Models;
using OpenLearning.Outcomes.Services;
using OpenLearning.Web.Pages.Courses.Outcomes;
using Xunit;

namespace OpenLearning.UnitTests.Web;

public sealed class OutcomesPageTests
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

    private static IndexModel CreatePage(ClaimsPrincipal user, ApplicationDbContext db, int courseId)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var page = new IndexModel(new OutcomeOperationsService(db), db)
        {
            PageContext = new PageContext(actionContext) { ViewData = viewData },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = new UrlHelper(actionContext),
            CourseId = courseId,
        };
        return page;
    }

    private static async Task<int> SeedCourseAsync(ApplicationDbContext db, string instructorId)
    {
        var course = new Course
        {
            Title = "Test course",
            InstructorId = instructorId,
            Status = CourseStatus.Published,
        };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return course.Id;
    }

    [Fact]
    public async Task OnGetAsync_OwnerInstructor_CanViewPage()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-1", Roles.Instructor), db, courseId);

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NonOwnerInstructor_IsForbidden()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-2", Roles.Instructor), db, courseId);

        var result = await page.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_Student_IsForbidden()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("student-1", Roles.Student), db, courseId);

        var result = await page.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostCreateOutcomeAsync_OwnerCreatesOutcome()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-1", Roles.Instructor), db, courseId);

        var result = await page.OnPostCreateOutcomeAsync("Explain ACID", "Given a scenario", 0.7);

        Assert.IsType<RedirectToPageResult>(result);
        var stored = await db.Set<CourseOutcome>().AsNoTracking().SingleOrDefaultAsync(o => o.CourseId == courseId);
        Assert.NotNull(stored);
        Assert.Equal("instructor-1", stored.OwnerId);
    }

    [Fact]
    public async Task OnPostCreateOutcomeAsync_NonOwner_IsForbidden()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-2", Roles.Instructor), db, courseId);

        var result = await page.OnPostCreateOutcomeAsync("Explain ACID", "Given a scenario", 0.7);

        Assert.IsType<ForbidResult>(result);
    }
}
