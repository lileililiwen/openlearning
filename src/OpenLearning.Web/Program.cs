using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Chat;
using OpenLearning.Chat.Hubs;
using OpenLearning.CourseManagement;
using OpenLearning.Data;
using OpenLearning.Ecommerce;
using OpenLearning.Enrollment;
using OpenLearning.Progress;
using OpenLearning.Scorm;
using OpenLearning.Scorm.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddRazorPages();
builder.Services.AddDataServices(connectionString);

// Identity is wired in the composition root because it needs the concrete
// ApplicationDbContext type from OpenLearning.Data, avoiding a circular
// Auth <-> Data project reference.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthModule();
builder.Services.AddCourseManagementModule();
builder.Services.AddEnrollmentModule();
builder.Services.AddProgressModule();
builder.Services.AddAssessmentsModule();
builder.Services.AddEcommerceModule();
builder.Services.AddScormModule();
builder.Services.AddChatModule();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<CourseChatHub>("/hubs/course-chat");

// SCORM 1.2 runtime API endpoints (called by scorm-api.js).
app.MapPost("/scorm/runtime/init", async (ScormInitRequest request, HttpContext http, ScormRuntimeService runtime) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var (enrollmentId, error) = await runtime.GetEnrollmentForPackageAsync(userId, request.PackageId);
    if (enrollmentId is null)
    {
        return Results.Json(new { error });
    }

    var state = await runtime.GetStateAsync(enrollmentId.Value, request.PackageId);
    return Results.Json(new
    {
        lessonLocation = state.LessonLocation,
        suspendData = state.SuspendData,
        lessonStatus = state.LessonStatus,
        scoreRaw = state.ScoreRaw,
        sessionTime = state.SessionTime,
    });
});

app.MapPost("/scorm/runtime/commit", async (ScormCommitRequest request, HttpContext http, ScormRuntimeService runtime) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var (enrollmentId, error) = await runtime.GetEnrollmentForPackageAsync(userId, request.PackageId);
    if (enrollmentId is null)
    {
        return Results.Json(new { error });
    }

    await runtime.CommitAsync(enrollmentId.Value, request.PackageId, new ScormRuntimeState(
        request.LessonLocation ?? string.Empty,
        request.SuspendData ?? string.Empty,
        request.LessonStatus ?? "not attempted",
        request.ScoreRaw ?? string.Empty,
        request.SessionTime ?? string.Empty));
    return Results.Json(new { ok = true });
});

// Apply migrations and seed demo data on first run (dev-friendly).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

record ScormInitRequest(int PackageId);

record ScormCommitRequest(
    int PackageId,
    string? LessonLocation,
    string? SuspendData,
    string? LessonStatus,
    string? ScoreRaw,
    string? SessionTime);
