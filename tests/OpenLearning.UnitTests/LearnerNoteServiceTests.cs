using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;
using Xunit;

namespace OpenLearning.UnitTests.StudyTools;

public sealed class LearnerNoteServiceTests
{
    private static (ApplicationDbContext Db, LearnerNoteService Service) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        return (db, new LearnerNoteService(db));
    }

    private static async Task<int> SeedCourseAsync(ApplicationDbContext db)
    {
        var course = new Course { Title = "Test Course", InstructorId = "i1" };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();
        return course.Id;
    }

    private static async Task<int> SeedLessonAsync(ApplicationDbContext db)
    {
        var lesson = new Lesson { Title = "Test Lesson" };
        db.Set<Lesson>().Add(lesson);
        await db.SaveChangesAsync();
        return lesson.Id;
    }

    private static NoteInput MakeInput(
        NoteContextType ctx = NoteContextType.Course,
        int contextId = 1,
        string body = "test note",
        string? tags = null)
    {
        return new(body, ctx, contextId, null, tags);
    }

    [Fact]
    public async Task Create_note_and_retrieve_it()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        var (id, error) = await service.CreateAsync("u1", MakeInput(contextId: courseId));
        Assert.True(error is null);
        Assert.True(id > 0);

        var note = await service.GetByIdAsync("u1", id);
        Assert.NotNull(note);
        Assert.Equal("test note", note.Body);
        Assert.Equal(courseId, note.ContextId);
    }

    [Fact]
    public async Task Update_note_body_and_tags()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        var (id, _) = await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "original"));
        var (ok, error) = await service.UpdateAsync("u1", id, MakeInput(contextId: courseId, body: "updated", tags: "tag1,tag2"));

        Assert.True(ok);
        Assert.True(error is null);
        var note = await service.GetByIdAsync("u1", id);
        Assert.NotNull(note);
        Assert.Equal("updated", note.Body);
        Assert.Equal("tag1,tag2", note.Tags);
    }

    [Fact]
    public async Task Delete_note()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        var (id, _) = await service.CreateAsync("u1", MakeInput(contextId: courseId));
        var ok = await service.DeleteAsync("u1", id);

        Assert.True(ok);
        Assert.Null(await service.GetByIdAsync("u1", id));
    }

    [Fact]
    public async Task List_notes_filtered_by_context_type()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);
        var lessonId = await SeedLessonAsync(db);

        await service.CreateAsync("u1", MakeInput(NoteContextType.Course, courseId, "course note"));
        await service.CreateAsync("u1", MakeInput(NoteContextType.Lesson, lessonId, "lesson note"));

        var courseNotes = await service.ListAsync("u1", NoteContextType.Course);
        var lessonNotes = await service.ListAsync("u1", NoteContextType.Lesson);

        Assert.Single(courseNotes);
        Assert.Equal("course note", courseNotes[0].Body);
        Assert.Single(lessonNotes);
        Assert.Equal("lesson note", lessonNotes[0].Body);
    }

    [Fact]
    public async Task Search_notes_by_body_text()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "Azure functions overview"));
        await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "Docker containers guide"));

        var results = await service.ListAsync("u1", search: "azure");
        Assert.Single(results);
        Assert.Contains("Azure", results[0].Body);
    }

    [Fact]
    public async Task Export_returns_all_notes_for_user()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "note one"));
        await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "note two"));
        await service.CreateAsync("u2", MakeInput(contextId: courseId, body: "other user note"));

        var entries = await service.ExportAsync("u1");
        Assert.Equal(2, entries.Count);
        var noteIds = entries.Select(e => e.Id).ToList();
        var userIds = await db.Set<LearnerNote>()
            .Where(n => noteIds.Contains(n.Id))
            .Select(n => n.UserId)
            .ToListAsync();
        Assert.All(userIds, uid => Assert.Equal("u1", uid));
    }

    [Fact]
    public void SanitizeMarkdown_strips_html_tags()
    {
        var result = LearnerNoteService.SanitizeMarkdown("<b>bold</b> and <script>alert('x')</script> clean");
        Assert.DoesNotContain("<b>", result);
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("bold", result);
        Assert.Contains("clean", result);
    }

    [Fact]
    public async Task Foreign_note_access_returns_not_found()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        var (id, _) = await service.CreateAsync("u1", MakeInput(contextId: courseId));
        var foreign = await service.GetByIdAsync("u2", id);

        Assert.Null(foreign);
    }

    [Fact]
    public async Task Empty_body_is_rejected()
    {
        var (db, service) = Create();
        var courseId = await SeedCourseAsync(db);

        var (id, error) = await service.CreateAsync("u1", MakeInput(contextId: courseId, body: "   "));
        Assert.Equal(0, id);
        Assert.NotNull(error);
    }
}
