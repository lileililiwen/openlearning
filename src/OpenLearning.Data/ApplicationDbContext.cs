using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Auth.Models;
using OpenLearning.Certificates.Models;
using OpenLearning.Chat.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Notifications.Models;
using OpenLearning.Progress.Models;
using OpenLearning.Ratings.Models;
using OpenLearning.Scorm.Models;
using OpenLearning.UserManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

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

    public DbSet<LessonAccess> LessonAccesses => Set<LessonAccess>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<ScormPackage> ScormPackages => Set<ScormPackage>();

    public DbSet<ScormRecord> ScormRecords => Set<ScormRecord>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<InstructorApplication> InstructorApplications => Set<InstructorApplication>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Each module owns its entities and ships its own IEntityTypeConfiguration
        // classes, so a new domain requires zero edits here.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUser).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Course).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(EnrollmentEntity).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LessonCompletion).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LessonAccess).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Quiz).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Order).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ScormPackage).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ChatMessage).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(InstructorApplication).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Review).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Certificate).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Notification).Assembly);
    }
}
