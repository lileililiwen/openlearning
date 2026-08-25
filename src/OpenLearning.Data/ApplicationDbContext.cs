using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenLearning.AI.Models;
using OpenLearning.Analytics.Models;
using OpenLearning.Assessments.Models;
using OpenLearning.Assignments.Models;
using OpenLearning.AsyncIO.Models;
using OpenLearning.Auth.Models;
using OpenLearning.Certificates.Models;
using OpenLearning.Chat.Models;
using OpenLearning.Classes.Models;
using OpenLearning.Competency.Models;
using OpenLearning.CouponIO.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseOutlineIO.Models;
using OpenLearning.Credits.Models;
using OpenLearning.Distribution.Models;
using OpenLearning.Ecommerce.Models;
using OpenLearning.Exams.Models;
using OpenLearning.Gamification.Models;
using OpenLearning.Gradebook.Models;
using OpenLearning.GradeExport.Models;
using OpenLearning.Invoicing.Models;
using OpenLearning.Jobs.Models;
using OpenLearning.LearningPaths.Models;
using OpenLearning.Live.Models;
using OpenLearning.Logging.Models;
using OpenLearning.Lti.Models;
using OpenLearning.Memberships.Models;
using OpenLearning.Mobile.Models;
using OpenLearning.Moderation.Models;
using OpenLearning.Notifications.Models;
using OpenLearning.Operations.Models;
using OpenLearning.Organizations.Models;
using OpenLearning.Payments.Models;
using OpenLearning.PeerAssessment.Models;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.Progress.Models;
using OpenLearning.QuestionIO.Models;
using OpenLearning.Ratings.Models;
using OpenLearning.Scorm.Models;
using OpenLearning.Settlement.Models;
using OpenLearning.Storage.Models;
using OpenLearning.StudentIO.Models;
using OpenLearning.StudyTools.Models;
using OpenLearning.Surveys.Models;
using OpenLearning.SystemConfig.Models;
using OpenLearning.UserManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using InvoiceRequestEntity = OpenLearning.Ecommerce.Models.InvoiceRequest;

namespace OpenLearning.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();

    public DbSet<LearningPathVersion> LearningPathVersions => Set<LearningPathVersion>();

    public DbSet<LearningPathStage> LearningPathStages => Set<LearningPathStage>();

    public DbSet<LearningPathCourse> LearningPathCourses => Set<LearningPathCourse>();

    public DbSet<PathEnrollment> PathEnrollments => Set<PathEnrollment>();

    public DbSet<Placement> PracticalPlacements => Set<Placement>();

    public DbSet<GamificationPointEntry> GamificationPointEntries => Set<GamificationPointEntry>();

    public DbSet<AiPolicy> AiPolicies => Set<AiPolicy>();

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<CourseTag> CourseTags => Set<CourseTag>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();

    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();

    public DbSet<LessonAccess> LessonAccesses => Set<LessonAccess>();

    public DbSet<StudySession> StudySessions => Set<StudySession>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<IntegrityPolicy> IntegrityPolicies => Set<IntegrityPolicy>();
    public DbSet<IntegritySession> IntegritySessions => Set<IntegritySession>();
    public DbSet<IntegrityEvidence> IntegrityEvidence => Set<IntegrityEvidence>();
    public DbSet<LearnerAccommodation> LearnerAccommodations => Set<LearnerAccommodation>();
    public DbSet<IntegrityIncident> IntegrityIncidents => Set<IntegrityIncident>();
    public DbSet<IntegrityDisposition> IntegrityDispositions => Set<IntegrityDisposition>();
    public DbSet<IntegrityAppeal> IntegrityAppeals => Set<IntegrityAppeal>();
    public DbSet<IntegrityAccessLog> IntegrityAccessLogs => Set<IntegrityAccessLog>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();

    public DbSet<BalanceLedger> BalanceLedgers => Set<BalanceLedger>();

    public DbSet<PointsLedger> PointsLedgers => Set<PointsLedger>();

    public DbSet<InvoiceRequestEntity> InvoiceRequests => Set<InvoiceRequestEntity>();

    public DbSet<SettlementLedger> SettlementLedgers => Set<SettlementLedger>();

    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();

    public DbSet<ScormPackage> ScormPackages => Set<ScormPackage>();

    public DbSet<ScormRecord> ScormRecords => Set<ScormRecord>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<InstructorApplication> InstructorApplications => Set<InstructorApplication>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<PhoneCode> PhoneCodes => Set<PhoneCode>();

    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Banner> Banners => Set<Banner>();

    public DbSet<Popup> Popups => Set<Popup>();

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<HomepageFeature> HomepageFeatures => Set<HomepageFeature>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();
    public DbSet<LearnerNote> LearnerNotes => Set<LearnerNote>();

    public DbSet<StudyCheckIn> StudyCheckIns => Set<StudyCheckIn>();

    public DbSet<LessonDownload> LessonDownloads => Set<LessonDownload>();

    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    public DbSet<ContentReport> ContentReports => Set<ContentReport>();

    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<LiveBooking> LiveBookings => Set<LiveBooking>();
    public DbSet<LiveWaitlist> LiveWaitlists => Set<LiveWaitlist>();
    public DbSet<LiveCalendarToken> LiveCalendarTokens => Set<LiveCalendarToken>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobRun> JobRuns => Set<JobRun>();

    public DbSet<QuestionImportJob> QuestionImportJobs => Set<QuestionImportJob>();

    public DbSet<QuestionRowError> QuestionRowErrors => Set<QuestionRowError>();

    public DbSet<StudentImportJob> StudentImportJobs => Set<StudentImportJob>();

    public DbSet<StudentImportRowError> StudentImportRowErrors => Set<StudentImportRowError>();

    public DbSet<GradeExportJob> GradeExportJobs => Set<GradeExportJob>();

    public DbSet<OutlineImportJob> OutlineImportJobs => Set<OutlineImportJob>();

    public DbSet<OutlineRowError> OutlineRowErrors => Set<OutlineRowError>();

    public DbSet<CouponImportJob> CouponImportJobs => Set<CouponImportJob>();

    public DbSet<CouponImportRowError> CouponImportRowErrors => Set<CouponImportRowError>();

    public DbSet<LtiRegistration> LtiRegistrations => Set<LtiRegistration>();

    public DbSet<CreditAward> CreditAwards => Set<CreditAward>();

    public DbSet<CourseCreditRule> CourseCreditRules => Set<CourseCreditRule>();

    public DbSet<GraduationProgram> GraduationPrograms => Set<GraduationProgram>();

    public DbSet<LearnerProgram> LearnerPrograms => Set<LearnerProgram>();

    public DbSet<GraduationDecision> GraduationDecisions => Set<GraduationDecision>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();

    public DbSet<OrganizationCourse> OrganizationCourses => Set<OrganizationCourse>();

    public DbSet<OrganizationAudit> OrganizationAudits => Set<OrganizationAudit>();

    public DbSet<DeviceSession> DeviceSessions => Set<DeviceSession>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OfflineManifest> OfflineManifests => Set<OfflineManifest>();

    public DbSet<OfflineManifestAsset> OfflineManifestAssets => Set<OfflineManifestAsset>();

    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();

    public DbSet<MobilePushDevice> MobilePushDevices => Set<MobilePushDevice>();

    public DbSet<LearningEvent> LearningEvents => Set<LearningEvent>();
    public DbSet<RefreshRun> RefreshRuns => Set<RefreshRun>();
    public DbSet<CourseFunnelAggregate> CourseFunnelAggregates => Set<CourseFunnelAggregate>();
    public DbSet<EngagementAggregate> EngagementAggregates => Set<EngagementAggregate>();
    public DbSet<CohortRetentionAggregate> CohortRetentionAggregates => Set<CohortRetentionAggregate>();
    public DbSet<AssessmentAggregate> AssessmentAggregates => Set<AssessmentAggregate>();
    public DbSet<WorkloadAggregate> WorkloadAggregates => Set<WorkloadAggregate>();
    public DbSet<ExportAudit> ExportAudits => Set<ExportAudit>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();

    public DbSet<PeerReviewConfig> PeerReviewConfigs => Set<PeerReviewConfig>();

    public DbSet<PeerReviewRubricQuestion> PeerReviewRubricQuestions => Set<PeerReviewRubricQuestion>();

    public DbSet<PeerAllocationRun> PeerAllocationRuns => Set<PeerAllocationRun>();

    public DbSet<PeerAllocationPair> PeerAllocationPairs => Set<PeerAllocationPair>();

    public DbSet<PeerReviewAssessment> PeerReviewAssessments => Set<PeerReviewAssessment>();

    public DbSet<PeerAssessmentAnswer> PeerAssessmentAnswers => Set<PeerAssessmentAnswer>();

    public DbSet<PeerReviewResult> PeerReviewResults => Set<PeerReviewResult>();

    public DbSet<CompetencyFramework> CompetencyFrameworks => Set<CompetencyFramework>();

    public DbSet<FrameworkScaleLevel> FrameworkScaleLevels => Set<FrameworkScaleLevel>();

    public DbSet<CompetencyNode> CompetencyNodes => Set<CompetencyNode>();

    public DbSet<ActivityMapping> ActivityMappings => Set<ActivityMapping>();

    public DbSet<CompetencyEvidence> CompetencyEvidence => Set<CompetencyEvidence>();

    public DbSet<GradebookConfig> GradebookConfigs => Set<GradebookConfig>();

    public DbSet<GradebookItem> GradebookItems => Set<GradebookItem>();

    public DbSet<GradebookAdjustment> GradebookAdjustments => Set<GradebookAdjustment>();

    public DbSet<GradebookSnapshot> GradebookSnapshots => Set<GradebookSnapshot>();

    public DbSet<Survey> Surveys => Set<Survey>();

    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();

    public DbSet<SurveyQuestionOption> SurveyQuestionOptions => Set<SurveyQuestionOption>();

    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();

    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();

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
        builder.ApplyConfigurationsFromAssembly(typeof(StoredFile).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(OperationLog).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Setting).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(MembershipPlan).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Banner).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Assignment).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LessonNote).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(SettlementLedger).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Exam).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ClassGroup).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(OpenLearning.Community.Models.Question).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ContentReport).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LiveSession).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Job).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DistributorProfile).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Invoice).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(AsyncIOJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(QuestionImportJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(StudentImportJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(GradeExportJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(OutlineImportJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(CouponImportJob).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LtiRegistration).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(CreditAward).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LearningPath).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Placement).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(GamificationPointEntry).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(AiPolicy).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(PaymentIntent).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Organization).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(LearningEvent).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DeviceSession).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(PeerReviewConfig).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(CompetencyFramework).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Survey).Assembly);
    }
}
