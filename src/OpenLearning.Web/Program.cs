using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.Certificates;
using OpenLearning.Chat;
using OpenLearning.Chat.Hubs;
using OpenLearning.CourseManagement;
using OpenLearning.Data;
using OpenLearning.Ecommerce;
using OpenLearning.Enrollment;
using OpenLearning.Logging;
using OpenLearning.Logging.Middleware;
using OpenLearning.Memberships;
using OpenLearning.Notifications;
using OpenLearning.Notifications.Channels;
using OpenLearning.Notifications.Email;
using OpenLearning.Operations;
using OpenLearning.Progress;
using OpenLearning.Ratings;
using OpenLearning.Scorm;
using OpenLearning.Scorm.Services;
using OpenLearning.Storage;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig;
using OpenLearning.UserManagement;
using OpenLearning.Web.Scorm;

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

// Third-party OAuth sign-in (Google/GitHub) — only configured providers
// appear on the login page. Unknown schemes are simply not added.
builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        });
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:GitHub:ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddOAuth("GitHub", options =>
        {
            options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;
            options.CallbackPath = "/signin-github";
            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";
            options.Scope.Add("user:email");

            // GitHub only returns the email via a separate endpoint; use the
            // profile name when email is unavailable.
            options.ClaimActions.MapJsonKey("urn:github:name", "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.Events.OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", context.AccessToken);
                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var email = user.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
                if (string.IsNullOrWhiteSpace(email) &&
                    user.RootElement.TryGetProperty("login", out var login))
                {
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Email, login.GetString() + "@users.noreply.github.com"));
                }
            };
        });
}

builder.Services.AddAuthModule();
builder.Services.AddCourseManagementModule();
builder.Services.AddEnrollmentModule();
builder.Services.AddProgressModule();
builder.Services.AddAssessmentsModule();
builder.Services.AddEcommerceModule();
builder.Services.AddScormModule();
builder.Services.AddChatModule();
builder.Services.AddUserManagementModule();
builder.Services.AddRatingsModule();
builder.Services.AddCertificatesModule();
builder.Services.AddNotificationsModule();
builder.Services.AddStorageModule(
    builder.Configuration["Storage:Root"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "storage"));
builder.Services.AddLoggingModule(builder.Configuration.GetValue("Logging:RetentionDays", 90));
builder.Services.AddMembershipsModule();
builder.Services.AddOperationsModule();
// After AddNotificationsModule so the template renderer override wins.
builder.Services.AddSystemConfigModule();
builder.Services.AddSignalR();

// Optional email channel: only used when Email:Enabled is true.
if (builder.Configuration.GetValue<bool>("Email:Enabled"))
{
    builder.Services.AddSingleton<IEmailSender>(_ => new SmtpEmailSender(
        builder.Configuration["Email:Host"] ?? "localhost",
        builder.Configuration.GetValue("Email:Port", 25),
        builder.Configuration["Email:From"] ?? "no-reply@openlearning.local",
        builder.Configuration["Email:User"],
        builder.Configuration["Email:Password"],
        builder.Configuration.GetValue("Email:UseSsl", false)));
}

// Optional SMS + web-push channels: only registered when enabled in config.
// The module's no-op defaults remain registered otherwise.
if (builder.Configuration.GetValue<bool>("Messaging:SmsEnabled"))
{
    builder.Services.AddSingleton<ISmsSender, SmsSender>();
}

if (builder.Configuration.GetValue<bool>("Messaging:PushEnabled") &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Messaging:VapidPublicKey"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Messaging:VapidPrivateKey"]))
{
    builder.Services.AddScoped<IWebPushSender>(sp => new WebPushSender(
        sp.GetRequiredService<DbContext>(),
        builder.Configuration["Messaging:VapidSubject"] ?? "mailto:no-reply@openlearning.local",
        builder.Configuration["Messaging:VapidPublicKey"]!,
        builder.Configuration["Messaging:VapidPrivateKey"]!,
        sp.GetRequiredService<ILogger<WebPushSender>>()));
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Persists unhandled exceptions to ErrorLog, then rethrows so the exception
// handler above still renders the error page.
app.UseMiddleware<LoggingExceptionMiddleware>();

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

// File storage serving. Private files (e.g. assignment answers) require the
// owner or an admin; public purposes stream to anyone. Keys contain slashes
// ({purpose}/{guid}{ext}) so the route is a catch-all.
app.MapGet("/files/{**key}", async (string key, HttpContext http, StorageService storage) =>
{
    var (file, stream) = await storage.OpenAsync(key);
    if (stream is null)
    {
        return Results.NotFound();
    }

    // Renditions have no StoredFile record; the ACL applies to recorded files
    // whose purpose is private.
    if (file?.IsPrivate == true)
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = http.User.IsInRole("Admin");
        if (userId is null || (userId != file.OwnerId && !isAdmin))
        {
            await stream.DisposeAsync();
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
    }

    var contentType = file?.ContentType ?? ContentTypeFor(key);
    return Results.File(stream, contentType, enableRangeProcessing: true);
});

static string ContentTypeFor(string key)
{
    var extension = Path.GetExtension(key).ToLowerInvariant();
    return extension switch
    {
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        _ => "application/octet-stream",
    };
}

app.MapGet("/files/{id:int}/renditions", async (int id, StorageService storage) =>
{
    var asset = await storage.GetRenditionsByIdAsync(id);
    if (asset is null)
    {
        return Results.NotFound();
    }

    return Results.Json(new
    {
        status = asset.Status.ToString().ToLowerInvariant(),
        low = asset.LowUrl,
        mid = asset.MidUrl,
        high = asset.HighUrl,
        error = asset.Error,
    });
});

// Apply migrations and seed demo data on first run (dev-friendly).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

await app.RunAsync();
