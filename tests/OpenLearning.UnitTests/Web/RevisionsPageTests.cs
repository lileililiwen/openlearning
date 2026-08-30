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
using OpenLearning.Authoring.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Web.Pages.Courses.Revisions;
using Xunit;

namespace OpenLearning.UnitTests.Web;

public sealed class RevisionsPageTests
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
        var page = new IndexModel(new RevisionLifecycleService(db), db)
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
    public async Task OnPostPublishAsync_OwnerPublishesDraft()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-1", Roles.Instructor), db, courseId);

        var service = new RevisionLifecycleService(db);
        await service.EnsureDraftAsync(courseId, "instructor-1");
        await service.EditDraftAsync(courseId, "instructor-1", new RevisionPatch
        {
            Title = "T",
            Summary = "S",
            Level = "Beginner",
            Language = "en",
            ContentSnapshotJson = "{\"Modules\":[{\"Title\":\"M1\",\"Lessons\":[{\"Title\":\"L1\",\"Type\":\"Video\"}]}]}",
        });

        var result = await page.OnPostPublishAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("success", page.TempData["MessageType"]?.ToString());
        var active = await service.GetActiveRevisionAsync(courseId);
        Assert.NotNull(active);
    }

    [Fact]
    public async Task OnPostPublishAsync_NonOwner_IsForbidden()
    {
        await using var db = NewDb();
        var courseId = await SeedCourseAsync(db, "instructor-1");
        var page = CreatePage(User("instructor-2", Roles.Instructor), db, courseId);

        var result = await page.OnPostPublishAsync();

        Assert.IsType<ForbidResult>(result);
    }
}
