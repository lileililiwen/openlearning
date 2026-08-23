using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;
using Xunit;

namespace OpenLearning.UnitTests;

public sealed class OrganizationTenancyTests
{
    [Fact]
    public async Task Forged_cross_tenant_department_id_is_not_disclosed_or_modified()
    {
        await using var db = CreateDb();
        db.AddRange(new Organization { Id = 1, Name = "One", Slug = "one", Status = OrganizationStatus.Active },
            new Organization { Id = 2, Name = "Two", Slug = "two", Status = OrganizationStatus.Active });
        db.Add(new Department { Id = 20, OrganizationId = 2, Name = "Secret" });
        await db.SaveChangesAsync();
        var service = new OrganizationService(db, new FakeContext(new(1, "One", "#000", OrganizationRole.OrganizationAdmin)));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MoveDepartmentAsync(20, null, "admin"));

        Assert.Equal("Department not found.", error.Message);
        Assert.Null((await db.Set<Department>().FindAsync(20))!.ParentId);
    }

    [Fact]
    public async Task Cyclic_department_move_is_rejected_without_changes()
    {
        await using var db = CreateDb();
        db.Add(new Organization { Id = 1, Name = "One", Slug = "one", Status = OrganizationStatus.Active });
        db.AddRange(new Department { Id = 1, OrganizationId = 1, Name = "Root" },
            new Department { Id = 2, OrganizationId = 1, Name = "Child", ParentId = 1 });
        await db.SaveChangesAsync();
        var service = new OrganizationService(db, new FakeContext(new(1, "One", "#000", OrganizationRole.OrganizationAdmin)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MoveDepartmentAsync(1, 2, "admin"));

        Assert.Null((await db.Set<Department>().FindAsync(1))!.ParentId);
    }

    [Fact]
    public async Task Scoped_queries_and_background_iteration_exclude_other_or_suspended_tenants()
    {
        await using var db = CreateDb();
        db.AddRange(new Organization { Id = 1, Name = "One", Slug = "one", Status = OrganizationStatus.Active },
            new Organization { Id = 2, Name = "Two", Slug = "two", Status = OrganizationStatus.Suspended });
        db.AddRange(new Course { Id = 10, Title = "One course", InstructorId = "owner" },
            new Course { Id = 20, Title = "Two course", InstructorId = "owner" });
        db.AddRange(new OrganizationCourse { OrganizationId = 1, CourseId = 10 },
            new OrganizationCourse { OrganizationId = 2, CourseId = 20 });
        await db.SaveChangesAsync();
        var service = new OrganizationService(db, new FakeContext(new(1, "One", "#000", OrganizationRole.OrganizationAdmin)));

        Assert.Equal(10, Assert.Single(await service.CoursesAsync()).CourseId);
        var visited = await service.RunForEachActiveAsync(Task.FromResult);
        Assert.Equal(1, Assert.Single(visited));
    }

    private static TestDb CreateDb()
    {
        return new(new DbContextOptionsBuilder<TestDb>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    private sealed class FakeContext(ActiveOrganization? active) : IOrganizationContext
    {
        public Task<ActiveOrganization?> GetActiveAsync()
        {
            return Task.FromResult(active);
        }

        public Task<bool> SetActiveAsync(int organizationId)
        {
            return Task.FromResult(active?.Id == organizationId);
        }

        public void Clear() { }
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>();
            modelBuilder.Entity<Department>().Ignore(x => x.Organization).Ignore(x => x.Parent);
            modelBuilder.Entity<OrganizationMembership>().Ignore(x => x.Organization).Ignore(x => x.User);
            modelBuilder.Entity<OrganizationInvitation>();
            modelBuilder.Entity<Course>().Ignore(x => x.Instructor).Ignore(x => x.Modules).Ignore(x => x.Tags);
            modelBuilder.Entity<OrganizationCourse>().HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
            modelBuilder.Entity<OrganizationAudit>();
        }
    }
}
