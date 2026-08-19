using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.CourseManagement;

public sealed class LessonServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task<(ApplicationDbContext Db, int ModuleId)> SeedModuleAsync()
    {
        var db = CreateDb();
        var course = new Course { Title = "C", InstructorId = "i1", Status = CourseStatus.Published };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        var module = new Module { Title = "M", CourseId = course.Id };
        db.Set<Module>().Add(module);
        await db.SaveChangesAsync();
        return (db, module.Id);
    }

    [Fact]
    public async Task AddAsync_persists_preview_flag_and_video_urls()
    {
        var (db, moduleId) = await SeedModuleAsync();
        var service = new LessonService(db);

        var lesson = await service.AddAsync(
            moduleId, "i1", "L1", "content", "/files/v.mp4", "/files/p.jpg", "/files/s.vtt", isPreview: true);

        Assert.NotNull(lesson);
        Assert.True(lesson.IsPreview);
        Assert.Equal("/files/v.mp4", lesson.VideoUrl);
        var stored = await db.Set<Lesson>().SingleAsync();
        Assert.True(stored.IsPreview);
        Assert.Equal("/files/s.vtt", stored.SubtitleUrl);
    }

    [Fact]
    public async Task AddAsync_rejects_non_owner()
    {
        var (db, moduleId) = await SeedModuleAsync();

        var lesson = await new LessonService(db).AddAsync(moduleId, "other", "L1", "content");

        Assert.Null(lesson);
        Assert.Empty(db.Set<Lesson>());
    }

    [Fact]
    public async Task UpdateAsync_persists_preview_flag_and_requires_owner()
    {
        var (db, moduleId) = await SeedModuleAsync();
        var service = new LessonService(db);
        var lesson = await service.AddAsync(moduleId, "i1", "L1", "content");
        Assert.NotNull(lesson);

        Assert.True(await service.UpdateAsync(lesson.Id, "i1", "L1", "content", isPreview: true));
        Assert.True((await db.Set<Lesson>().SingleAsync()).IsPreview);

        Assert.False(await service.UpdateAsync(lesson.Id, "other", "L1", "content"));
        Assert.True((await db.Set<Lesson>().SingleAsync()).IsPreview); // unchanged
    }
}
