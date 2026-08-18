# Initial LMS MVP — Design

## Context

We are creating a new MIT-licensed online learning system in C#/.NET 8 with PostgreSQL. The MVP targets authentication with roles, course delivery, enrollment, and progress tracking. No existing code in the repository; OpenSpec scaffolding already present. The design is informed by (but not copied from) MIT-licensed references: CoreLMS, SmartLearning, LearnNest. Per the user's requirement, these are credited in the README.

## Goals

- Shippable MVP with a single web application (monolith).
- Clean separation of concerns: domain models, EF Core persistence, Razor Pages UI.
- Role-based access using ASP.NET Core Identity policies.
- PostgreSQL as the database.
- MIT license with documented attribution.

## Non-Goals

- No SCORM/LTI support (future capability).
- No ecommerce/payments.
- No Blazor SPA, live video, chat, or ML features.
- No microservices.

## Decisions

### D1: ASP.NET Core Razor Pages over MVC or Blazor
Server-rendered Razor Pages with built-in auth scaffolding. Fastest to ship, SEO-friendly, zero frontend build. Blazor can be layered later without replacing the domain layer.

### D2: EF Core + Npgsql for persistence
Code-first migrations with PostgreSQL. Matches the user's database choice and the reference projects' EF Core pattern.

### D3: ASP.NET Core Identity for auth
Provides registration, login, roles, and claim-based policies out of the box. Seed roles: Student (default), Instructor, Admin.

### D4: Domain model
- `ApplicationUser : IdentityUser` — seeded role assignment via `UserManager`.
- `Course { Id, Title, Description, Category, Status, InstructorId, CreatedAt, Modules }`
- `Module { Id, CourseId, Title, OrderIndex, Lessons }`
- `Lesson { Id, ModuleId, Title, Content (markdown), OrderIndex }`
- `Enrollment { Id, StudentId, CourseId, EnrolledAt, Unique(StudentId, CourseId) }`
- `LessonCompletion { Id, EnrollmentId, LessonId, CompletedAt, Unique(EnrollmentId, LessonId) }`

### D5: Modular monolith — one package per business domain
Each OpenSpec capability maps to a dedicated class library (modular monolith), so a new spec/domain lives in a new package without touching existing code. This mirrors SolenLMS's modular architecture.

- `OpenLearning.Auth` — Identity user, role policies, account services.
- `OpenLearning.CourseManagement` — Course/Module/Lesson aggregate + management services.
- `OpenLearning.Enrollment` — enrollment records + services (depends on CourseManagement).
- `OpenLearning.Progress` — lesson completions + progress calculation (depends on Enrollment).
- `OpenLearning.Data` — central `ApplicationDbContext`, entity configurations discovered via `ApplyConfigurationsFromAssembly`, migrations, seeding.
- `OpenLearning.Web` — Razor Pages UI shell (feature folders under `Pages/`), DI composition root.

To avoid circular references, `ApplicationUser` carries no navigation collections to courses/enrollments; cross-aggregate queries go through module services. Entity configurations ship inside each module; the central DbContext scans module assemblies so a new domain requires zero edits to it. A new module registers itself via a single `AddXxxModule(this IServiceCollection)` call in `Program.cs`.

### D6: Seeding
On startup, if the database is empty: create roles, seed Admin + Instructor + Student users, and one published sample course with modules/lessons.

### D7: Progress calculation
`Progress = CompletedLessonCount / TotalLessonCount` computed on demand via the service layer; no denormalized storage needed at MVP scale.

## Risks / Trade-offs

- **Single-project structure may grow messy** → Encapsulate business logic in `Services/`, keep Razor Pages thin; split projects when scope demands.
- **Markdown rendering dependency** → Use a small, MIT-compatible markdown library or render plain text; avoid heavy dependencies at MVP stage.
- **Seed credentials are public** → Clearly document them in README and force change in production.

## Migration Plan

Greenfield; no existing data. Apply `dotnet ef database update` on first run or auto-migrate on startup for demo friendliness.

## Open Questions

- Content format for lessons (markdown vs rich text) — MVP uses markdown; revisit for instructor UX.
- Category model: free-text tag vs normalized entity — MVP uses free-text category string.