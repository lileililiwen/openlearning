using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using OpenLearning.Progress.Models;

namespace OpenLearning.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();

    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Each module owns its entities and ships its own IEntityTypeConfiguration
        // classes, so a new domain requires zero edits here.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUser).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Course).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(EnrollmentEntity).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LessonCompletion).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Quiz).Assembly);
    }
}
