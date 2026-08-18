using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement;
using OpenLearning.Data;
using OpenLearning.Enrollment;
using OpenLearning.Progress;

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

// Apply migrations and seed demo data on first run (dev-friendly).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
