using Microsoft.EntityFrameworkCore;
using OpenLearning.Chat.Models;
using OpenLearning.Chat.Services;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Chat;

public sealed class ChatServiceTests
{
    private static (ApplicationDbContext Db, int CourseId, int LessonId) Seed()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var course = new Course
        {
            Title = "C1",
            InstructorId = "i1",
            Status = CourseStatus.Published,
            Modules = new List<Module>
            {
                new()
                {
                    Title = "M1",
                    Lessons = new List<Lesson> { new() { Title = "L1" } },
                },
            },
        };
        db.Set<Course>().Add(course);
        db.SaveChanges();
        var lessonId = course.Modules.SelectMany(m => m.Lessons).Single().Id;
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id });
        db.SaveChanges();
        return (db, course.Id, lessonId);
    }

    [Fact]
    public async Task AddDanmu_stores_a_danmu_message_for_the_lesson()
    {
        var (db, courseId, lessonId) = Seed();
        var service = new ChatService(db);

        var message = await service.AddDanmuAsync(courseId, lessonId, "s1", "great video!");

        Assert.NotNull(message);
        Assert.Equal(ChatService.DanmuType, message.Type);
        Assert.Equal(lessonId, message.LessonId);
        Assert.Equal("great video!", message.Body);
        Assert.Single(db.Set<ChatMessage>());
    }

    [Fact]
    public async Task AddDanmu_rejects_non_participant_and_foreign_lesson()
    {
        var (db, courseId, lessonId) = Seed();
        var service = new ChatService(db);

        var nonParticipant = await service.AddDanmuAsync(courseId, lessonId, "other", "hi");
        var foreignLesson = await service.AddDanmuAsync(courseId, 999_999, "s1", "hi");

        Assert.Null(nonParticipant);
        Assert.Null(foreignLesson);
        Assert.Empty(db.Set<ChatMessage>());
    }

    [Fact]
    public async Task GetLessonDanmu_returns_only_danmu_for_the_lesson_oldest_first()
    {
        var (db, courseId, lessonId) = Seed();
        var service = new ChatService(db);
        await service.AddDanmuAsync(courseId, lessonId, "s1", "first");
        db.Set<ChatMessage>().Add(new ChatMessage { CourseId = courseId, UserId = "s1", Body = "ignored chat" });
        await db.SaveChangesAsync();

        var messages = await service.GetLessonDanmuAsync(lessonId);

        var item = Assert.Single(messages);
        Assert.Equal("first", item.Body);
    }
}
