## Why

The open-source C# LMS space has no modern, actively-maintained, MIT-licensed option that combines a clean architecture with course delivery, enrollment, and progress tracking. Existing projects are either stale (Neddle, LGPL, last active 2014), feature-aspirational (LearnSphere), or narrow-scoped. This change creates a genuinely usable, MIT-licensed MVP that fills that gap.

## What Changes

- Scaffold a new .NET 8 solution `OpenLearning` in `/home/paul/code/openlearning` with MIT license.
- Build an MVP with:
  - ASP.NET Core Identity auth with three roles: Student, Instructor, Admin.
  - Course catalog with create/edit/publish lifecycle managed by Instructors.
  - Hierarchical content model: Course → Module → Lesson.
  - Enrollment flow (Students enroll, cannot enroll twice).
  - Lesson completion + per-course progress percentage.
  - Role-gated UI (Razor Pages) for students, instructors, and admins.
  - PostgreSQL persistence via EF Core (Npgsql).
- Design informed by MIT-licensed open-source references (CoreLMS, SmartLearning, LearnNest); credits documented in README per open-source spirit.
- Define specs, design, and tasks via OpenSpec before implementation.

## Capabilities

### New Capabilities

- `user-auth`: Registration, login/logout, and role-based access control (Student/Instructor/Admin) via ASP.NET Core Identity.
- `course-management`: Course CRUD with publish/unpublish lifecycle, owned by Instructors, visible to Students when published.
- `course-structure`: Course → Module → Lesson hierarchical content model with ordering.
- `enrollment`: Students enroll in published courses; duplicate enrollments are prevented; enrollment state tracked.
- `progress-tracking`: Lesson completion marks, per-course progress percentage, and per-student progress visibility.
- `lms-core`: Shared application shell (layout, navigation, error handling, seeding) and MIT license.

### Modified Capabilities

None.

## Impact

- New repository at `/home/paul/code/openlearning` (currently empty aside from OpenSpec scaffolding).
- New .NET 8 solution; dependencies: ASP.NET Core, EF Core + Npgsql, ASP.NET Core Identity.
- No existing code to migrate; PostgreSQL required at runtime.
- README credits MIT-licensed reference projects (CoreLMS, SmartLearning, LearnNest).