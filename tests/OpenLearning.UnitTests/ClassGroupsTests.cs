using Microsoft.EntityFrameworkCore;
using OpenLearning.Classes.Models;
using OpenLearning.Classes.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Classes;

public sealed class ClassGroupsTests
{
    private static async Task<(ApplicationDbContext Db, int CourseId)> SeedAsync()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return (db, course.Id);
    }

    [Fact]
    public async Task Create_enforces_ownership_and_end_after_start()
    {
        var (db, courseId) = await SeedAsync();
        var service = new ClassGroupService(db);

        var (_, error) = await service.CreateAsync(courseId, "other", "X", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null);
        Assert.NotNull(error);

        var (_, badWindow) = await service.CreateAsync(courseId, "i1", "X", DateTime.UtcNow.AddDays(1), DateTime.UtcNow, null);
        Assert.NotNull(badWindow);

        var (created, okError) = await service.CreateAsync(courseId, "i1", "2026 Spring", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(30), null);
        Assert.Null(okError);
        Assert.NotNull(created);
        Assert.Equal(ClassGroupStatus.Upcoming, created.EffectiveStatus);
    }

    [Fact]
    public async Task Assign_is_unique_and_enroll_respects_capacity()
    {
        var (db, courseId) = await SeedAsync();
        var groups = new ClassGroupService(db);
        var assignments = new ClassAssignmentService(db);

        var (classGroup, _) = await groups.CreateAsync(courseId, "i1", "2026 Spring", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), capacity: 1);
        Assert.NotNull(classGroup);

        Assert.True((await assignments.AssignAsync(classGroup.Id, "i1", "ta1", ClassAssignmentRole.TeachingAssistant)).Ok);
        Assert.False((await assignments.AssignAsync(classGroup.Id, "i1", "ta1", ClassAssignmentRole.TeachingAssistant)).Ok);
        Assert.True((await assignments.AssignAsync(classGroup.Id, "i1", "ta1", ClassAssignmentRole.Observer)).Ok);

        var e1 = new EnrollmentEntity { StudentId = "s1", CourseId = courseId };
        var e2 = new EnrollmentEntity { StudentId = "s2", CourseId = courseId };
        db.Set<EnrollmentEntity>().AddRange(e1, e2);
        await db.SaveChangesAsync();

        Assert.True((await groups.EnrollIntoClassAsync(classGroup.Id, e1.Id, "i1")).Ok);
        var (ok2, error2) = await groups.EnrollIntoClassAsync(classGroup.Id, e2.Id, "i1");
        Assert.False(ok2);
        Assert.Contains("capacity", error2, System.StringComparison.OrdinalIgnoreCase);
    }
}
