using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenLearning.Assessments.Models;
using OpenLearning.Assignments.Models;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Certificates.Models;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Models;
using OpenLearning.GradeExport.Jobs;
using OpenLearning.GradeExport.Models;
using OpenLearning.GradeExport.Services;
using OpenLearning.Jobs;
using OpenLearning.Logging.Services;
using OpenLearning.Progress.Services;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.GradeExport;

public sealed class GradeExportTests
{
    private static (ApplicationDbContext Db, GradeExportService Service, IClassAssignmentLookup Lookup, StorageService Storage, string TempDir) Create(
        IClassAssignmentLookup? lookup = null)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = Path.Combine(Path.GetTempPath(), "ol-gradeexport-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var asyncIO = new AsyncIOService(db, storage, TestNotificationService.Create(db));
        if (lookup is null)
        {
            var defaultLookup = new Mock<IClassAssignmentLookup>();
            defaultLookup.Setup(l => l.IsAssignedAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
            lookup = defaultLookup.Object;
        }

        var service = new GradeExportService(db, asyncIO, new ProgressService(db), lookup, new LogService(db));
        return (db, service, lookup, storage, tempDir);
    }

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext db, string id, string email, string displayName)
    {
        var user = new ApplicationUser { Id = id, UserName = email, Email = email, DisplayName = displayName };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Course> SeedCourseAsync(ApplicationDbContext db, string instructorId, string title = "Course")
    {
        var now = DateTime.UtcNow;
        var course = new Course
        {
            Title = title,
            InstructorId = instructorId,
            Status = CourseStatus.Published,
            Description = "description",
            Category = "category",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    private static List<string[]> ReadRows(byte[] xlsx)
    {
        using var workbook = new XLWorkbook(new MemoryStream(xlsx));
        var sheet = workbook.Worksheets.First();
        var rows = new List<string[]>();
        for (var row = 1; row <= sheet.LastRowUsed()?.RowNumber(); row++)
        {
            var values = new List<string>();
            for (var col = 1; col <= sheet.LastColumnUsed()?.ColumnNumber(); col++)
            {
                values.Add(sheet.Cell(row, col).GetFormattedString());
            }

            rows.Add(values.ToArray());
        }

        return rows;
    }

    private static async Task<Assignment> SeedAssignmentAsync(ApplicationDbContext db, int courseId, string instructorId, string title = "A1")
    {
        var assignment = new Assignment { CourseId = courseId, AuthorId = instructorId, Title = title, Instructions = "do it" };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment;
    }

    private static async Task<Quiz> SeedQuizAsync(ApplicationDbContext db, int courseId)
    {
        var quiz = new Quiz { CourseId = courseId, Title = "Quiz1", OrderIndex = 1 };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();
        return quiz;
    }

    private static async Task<Exam> SeedExamAsync(ApplicationDbContext db, int courseId)
    {
        var exam = new Exam { CourseId = courseId, AuthorId = "inst", Title = "Exam1", Description = "d" };
        db.Set<Exam>().Add(exam);
        await db.SaveChangesAsync();
        return exam;
    }

    private static async Task<(Question Question, AnswerOption Correct)> SeedSingleChoiceAsync(ApplicationDbContext db, int quizId)
    {
        var question = new Question
        {
            QuizId = quizId,
            Text = "Q?",
            QuestionType = QuestionType.SingleChoice,
            OrderIndex = 1,
            Points = 1,
        };
        var correct = new AnswerOption { Text = "A", IsCorrect = true, OrderIndex = 1 };
        question.AnswerOptions.Add(correct);
        question.AnswerOptions.Add(new AnswerOption { Text = "B", IsCorrect = false, OrderIndex = 2 });
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return (question, correct);
    }

    private static async Task<QuizAttempt> SeedQuizAttemptAsync(ApplicationDbContext db, Quiz quiz, Question question, AnswerOption correct, string studentId)
    {
        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            Quiz = quiz,
            StudentId = studentId,
            CompletedAt = DateTime.UtcNow,
            Score = 1,
            MaxScore = 1,
        };
        attempt.Answers.Add(new QuizAttemptAnswer
        {
            QuestionId = question.Id,
            Question = question,
            AnswerOptionId = correct.Id,
            IsCorrect = true,
        });
        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync();
        return attempt;
    }

    private static async Task<ExamAttempt> SeedExamAttemptAsync(ApplicationDbContext db, Exam exam, string studentId)
    {
        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            Exam = exam,
            StudentId = studentId,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            SubmittedAt = DateTime.UtcNow,
            Score = 8,
            MaxScore = 10,
            Percent = 80,
            Passed = true,
            ScreenSwitchCount = 1,
            Status = ExamAttemptStatus.Completed,
        };
        db.Set<ExamAttempt>().Add(attempt);
        await db.SaveChangesAsync();
        return attempt;
    }

    // ===== Submissions =====

    [Fact]
    public async Task ExportSubmissions_owner_returns_only_owned_rows()
    {
        var (db, service, _, _, _) = Create();
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        await SeedUserAsync(db, "s2", "s2@example.com", "Student Two");
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        var other = await SeedUserAsync(db, "other", "other@example.com", "Other");
        var course = await SeedCourseAsync(db, "inst");
        var otherCourse = await SeedCourseAsync(db, "other");
        var assignment = await SeedAssignmentAsync(db, course.Id, instructor.Id, "A1");
        var otherAssignment = await SeedAssignmentAsync(db, otherCourse.Id, other.Id, "A2");
        db.AssignmentSubmissions.AddRange(
            new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s1", Text = "hi", SubmittedAt = DateTime.UtcNow.AddDays(-1) },
            new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s2", Text = "there", SubmittedAt = DateTime.UtcNow },
            new AssignmentSubmission { AssignmentId = otherAssignment.Id, StudentId = "s1", Text = "other", SubmittedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(null, assignment.Id, null, null, null, null, null, null, false, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.Submissions, filters, "inst");

        Assert.Null(error);
        Assert.Equal(2, rowCount);
        var rows = ReadRows(bytes!);
        Assert.Equal(3, rows.Count); // header + 2 data rows
        Assert.Equal("StudentEmail", rows[0][0]);
        Assert.Equal("s1@example.com", rows[1][0]);
        Assert.Equal("Student One", rows[1][1]);
        Assert.Equal("A1", rows[1][2]);
    }

    [Fact]
    public async Task ExportSubmissions_non_owner_denied()
    {
        var (db, service, _, _, _) = Create();
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        var other = await SeedUserAsync(db, "other", "other@example.com", "Other");
        var course = await SeedCourseAsync(db, "inst");
        var assignment = await SeedAssignmentAsync(db, course.Id, instructor.Id);
        db.AssignmentSubmissions.Add(new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s1", Text = "hi", SubmittedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(null, assignment.Id, null, null, null, null, null, null, false, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.Submissions, filters, other.Id);

        Assert.Null(bytes);
        Assert.NotNull(error);
        Assert.Equal(0, rowCount);
    }

    [Fact]
    public async Task CountSubmissions_honors_date_and_status_filters()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "S");
        var course = await SeedCourseAsync(db, instructor.Id);
        var assignment = await SeedAssignmentAsync(db, course.Id, instructor.Id);
        db.AssignmentSubmissions.AddRange(
            new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s1", Text = "a", SubmittedAt = DateTime.UtcNow.AddDays(-10), Score = 90, GradedAt = DateTime.UtcNow },
            new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s1", Text = "b", SubmittedAt = DateTime.UtcNow, Score = null });
        await db.SaveChangesAsync();

        var from = new GradeExportFilters(null, assignment.Id, null, null, null, DateTime.UtcNow.AddDays(-5), null, null, false, false);
        var graded = new GradeExportFilters(null, assignment.Id, null, null, null, null, null, true, false, false);

        Assert.Equal(1, await service.CountAsync(GradeExportKind.Submissions, from, "inst"));
        Assert.Equal(1, await service.CountAsync(GradeExportKind.Submissions, graded, "inst"));
    }

    // ===== Quiz attempts =====

    [Fact]
    public async Task ExportQuizAttempts_per_quiz_includes_per_question_json()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var quiz = await SeedQuizAsync(db, course.Id);
        var (question, correct) = await SeedSingleChoiceAsync(db, quiz.Id);
        await SeedQuizAttemptAsync(db, quiz, question, correct, "s1");

        var filters = new GradeExportFilters(course.Id, null, quiz.Id, null, null, null, null, null, false, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.QuizAttempts, filters, instructor.Id);

        Assert.Null(error);
        Assert.Equal(1, rowCount);
        var rows = ReadRows(bytes!);
        Assert.Equal("Quiz1", rows[1][2]);
        Assert.Equal("100", rows[1][4]);
        Assert.Equal("Yes", rows[1][5]);
        Assert.Contains("Q?", rows[1][6]);
        Assert.Contains("A", rows[1][6]);
    }

    [Fact]
    public async Task ExportQuizAttempts_course_wide_and_denies_non_owner()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        var other = await SeedUserAsync(db, "other", "other@example.com", "Other");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var quiz = await SeedQuizAsync(db, course.Id);
        var (question, correct) = await SeedSingleChoiceAsync(db, quiz.Id);
        await SeedQuizAttemptAsync(db, quiz, question, correct, "s1");

        var filters = new GradeExportFilters(course.Id, null, null, null, null, null, null, null, false, false);
        Assert.Equal(1, await service.CountAsync(GradeExportKind.QuizAttempts, filters, instructor.Id));

        var (bytes, error, _) = await service.ExportSyncAsync(GradeExportKind.QuizAttempts, filters, other.Id);
        Assert.Null(bytes);
        Assert.NotNull(error);
    }

    // ===== Exam attempts =====

    [Fact]
    public async Task ExportExamAttempts_includes_result_columns()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var exam = await SeedExamAsync(db, course.Id);
        await SeedExamAttemptAsync(db, exam, "s1");

        var filters = new GradeExportFilters(course.Id, null, null, exam.Id, null, null, null, null, false, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.ExamAttempts, filters, instructor.Id);

        Assert.Null(error);
        Assert.Equal(1, rowCount);
        var rows = ReadRows(bytes!);
        Assert.Equal("Exam1", rows[1][2]);
        Assert.Equal("80", rows[1][5]);
        Assert.Equal("Yes", rows[1][6]);
        Assert.Equal("1", rows[1][7]);
    }

    // ===== Roster =====

    [Fact]
    public async Task ExportRoster_includes_certificate_and_progress()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var enrollment = new EnrollmentEntity { StudentId = "s1", CourseId = course.Id, EnrolledAt = DateTime.UtcNow.AddDays(-5) };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        db.Certificates.Add(new Certificate { EnrollmentId = enrollment.Id, CourseId = course.Id, UserId = "s1", Code = "CRT-123" });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(course.Id, null, null, null, null, null, null, null, false, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.CourseRoster, filters, instructor.Id);

        Assert.Null(error);
        Assert.Equal(1, rowCount);
        var rows = ReadRows(bytes!);
        Assert.Equal("s1@example.com", rows[1][0]);
        Assert.Equal("Student One", rows[1][1]);
        Assert.Equal("CRT-123", rows[1][6]);
    }

    [Fact]
    public async Task ExportClassRoster_ta_denied_for_unassigned_class()
    {
        var lookup = new Mock<IClassAssignmentLookup>();
        lookup.Setup(l => l.IsAssignedAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
        var (db, service, _, _, _) = Create(lookup.Object);
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        var course = await SeedCourseAsync(db, instructor.Id);
        var classGroup = new ClassGroup { CourseId = course.Id, Name = "C1", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddMonths(1) };
        db.Set<ClassGroup>().Add(classGroup);
        await db.SaveChangesAsync();
        db.Enrollments.Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id, ClassGroupId = classGroup.Id, EnrolledAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(null, null, null, null, classGroup.Id, null, null, null, true, false);
        Assert.Equal(0, await service.CountAsync(GradeExportKind.CourseRoster, filters, "ta1"));
    }

    [Fact]
    public async Task ExportClassRoster_ta_includes_assigned_class_rows()
    {
        var (db, service, _, _, _) = Create();
        var ta = await SeedUserAsync(db, "ta1", "ta@example.com", "TA");
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var classGroup = new ClassGroup { CourseId = course.Id, Name = "C1", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddMonths(1) };
        db.Set<ClassGroup>().Add(classGroup);
        await db.SaveChangesAsync();
        db.Enrollments.Add(new EnrollmentEntity { StudentId = "s1", CourseId = course.Id, ClassGroupId = classGroup.Id, EnrolledAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(null, null, null, null, classGroup.Id, null, null, null, true, false);
        var (bytes, error, rowCount) = await service.ExportSyncAsync(GradeExportKind.CourseRoster, filters, ta.Id);

        Assert.Null(error);
        Assert.Equal(1, rowCount);
        var rows = ReadRows(bytes!);
        Assert.Equal("s1@example.com", rows[1][0]);
    }

    // ===== Async path =====

    [Fact]
    public async Task SubmitAndProcess_async_job_stores_file_and_marks_completed()
    {
        var (db, service, _, _, _) = Create();
        var instructor = await SeedUserAsync(db, "inst", "inst@example.com", "Inst");
        await SeedUserAsync(db, "s1", "s1@example.com", "Student One");
        var course = await SeedCourseAsync(db, instructor.Id);
        var assignment = await SeedAssignmentAsync(db, course.Id, instructor.Id);
        db.AssignmentSubmissions.Add(new AssignmentSubmission { AssignmentId = assignment.Id, StudentId = "s1", Text = "hi", SubmittedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var filters = new GradeExportFilters(null, assignment.Id, null, null, null, null, null, null, false, false);
        var (jobId, error) = await service.SubmitAsync(GradeExportKind.Submissions, filters, instructor.Id);
        Assert.NotNull(jobId);
        Assert.Null(error);

        var job = await db.Set<AsyncIOJob>().FirstAsync(j => j.Id == jobId);
        var (ok, processError, total, success) = await service.ProcessAsync(job, null, default);
        Assert.True(ok);
        Assert.Null(processError);
        Assert.Equal(1, total);
        Assert.Equal(1, success);

        var record = await db.GradeExportJobs.FirstAsync();
        Assert.Equal(GradeExportJobStatus.Completed, record.Status);
        Assert.NotNull(record.FileKey);
        Assert.Equal(1, record.RowCount);
    }

    [Fact]
    public async Task CleanupJob_prunes_expired_files()
    {
        var (db, _, _, storage, _) = Create();
        var config = new SystemConfigService(db);
        var job = new GradeExportCleanupJob(db, storage, config);
        db.GradeExportJobs.Add(new GradeExportJob
        {
            UserId = "inst",
            Kind = GradeExportKind.CourseRoster,
            Status = GradeExportJobStatus.Completed,
            FileKey = "async-io/old-file.xlsx",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        });
        db.GradeExportJobs.Add(new GradeExportJob
        {
            UserId = "inst",
            Kind = GradeExportKind.CourseRoster,
            Status = GradeExportJobStatus.Completed,
            FileKey = "async-io/recent-file.xlsx",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await job.ExecuteAsync(new JobContext(1, "key"), default);

        var old = await db.GradeExportJobs.FirstAsync(g => g.CreatedAt < DateTime.UtcNow.AddDays(-10));
        var recent = await db.GradeExportJobs.FirstAsync(g => g.CreatedAt >= DateTime.UtcNow.AddDays(-10));
        Assert.Null(old.FileKey);
        Assert.NotNull(recent.FileKey);
    }
}
