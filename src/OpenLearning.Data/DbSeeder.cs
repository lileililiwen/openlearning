using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Auth;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;
using OpenLearning.Progress.Models;

namespace OpenLearning.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Instructor, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await EnsureUserAsync(userManager, "admin@openlearning.dev", "Admin123!", "Admin", Roles.Admin);
        var instructor = await EnsureUserAsync(userManager, "instructor@openlearning.dev", "Instructor123!", "Instructor", Roles.Instructor);
        var student = await EnsureUserAsync(userManager, "student@openlearning.dev", "Student123!", "Student", Roles.Student);

        if (!await db.Courses.AnyAsync())
        {
            var course = new Course
            {
                Title = "Introduction to C# Programming",
                Description =
                    "A hands-on introduction to C# for absolute beginners. Learn the language " +
                    "basics, then build your first console applications step by step.",
                Category = "Programming",
                Status = CourseStatus.Published,
                InstructorId = instructor.Id,
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var moduleOne = new Module { CourseId = course.Id, Title = "Getting Started", OrderIndex = 1 };
            var moduleTwo = new Module { CourseId = course.Id, Title = "Core Concepts", OrderIndex = 2 };
            db.Modules.AddRange(moduleOne, moduleTwo);
            await db.SaveChangesAsync();

            db.Lessons.AddRange(
                new Lesson
                {
                    ModuleId = moduleOne.Id,
                    Title = "What is C#?",
                    OrderIndex = 1,
                    Content =
                        "C# is a modern, object-oriented programming language developed by Microsoft.\n\n" +
                        "- Runs on .NET\n- Strongly typed\n- Used for web, desktop, mobile and cloud apps\n\n" +
                        "This course assumes no prior programming experience.",
                },
                new Lesson
                {
                    ModuleId = moduleOne.Id,
                    Title = "Installing the .NET SDK",
                    OrderIndex = 2,
                    Content =
                        "1. Download the .NET SDK from https://dotnet.microsoft.com\n" +
                        "2. Run the installer\n3. Open a terminal and type `dotnet --version`\n\n" +
                        "If you see a version number, you are ready to go!",
                },
                new Lesson
                {
                    ModuleId = moduleTwo.Id,
                    Title = "Variables and Types",
                    OrderIndex = 1,
                    Content =
                        "A variable stores a value. Every variable has a type:\n\n" +
                        "```csharp\nint age = 30;\nstring name = \"Ada\";\ndouble score = 98.5;\nbool enrolled = true;\n```\n\n" +
                        "The compiler checks that you use each variable consistently.",
                });
            await db.SaveChangesAsync();

            // Demo: enroll the seeded student so "My Courses" and progress are
            // visible immediately on first run.
            var enrollment = new EnrollmentEntity { StudentId = student.Id, CourseId = course.Id };
            db.Enrollments.Add(enrollment);
            await db.SaveChangesAsync();

            var lessonIds = await db.Lessons.Select(l => l.Id).ToListAsync();
            if (lessonIds.Count >= 2)
            {
                db.LessonCompletions.AddRange(
                    new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = lessonIds[0] },
                    new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = lessonIds[1] });
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
