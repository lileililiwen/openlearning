# Initial LMS MVP — Tasks

## 1. Project Setup

- [x] 1.1 Create `.gitignore`, `LICENSE` (MIT), and `README.md` with acknowledgments
- [x] 1.2 Create .NET 8 solution and Razor Pages web project with PostgreSQL EF Core packages
- [x] 1.3 Add ASP.NET Core Identity, authentication, and role policies (Student/Instructor/Admin)
- [x] 1.4 Configure connection string and ensure PostgreSQL migrations run

## 2. Data Model

- [x] 2.1 Add domain entities: Course, Module, Lesson, Enrollment, LessonCompletion
- [x] 2.2 Configure DbContext with relationships, indexes, and unique constraints
- [x] 2.3 Create initial EF Core migration

## 3. Auth & Roles

- [x] 3.1 Implement Register, Login, Logout pages
- [x] 3.2 Seed roles and demo users (Admin, Instructor, Student)
- [x] 3.3 Enforce role policies on pages

## 4. Course Management

- [x] 4.1 Implement course catalog page (published courses, public)
- [x] 4.2 Implement course create/edit/delete for Instructors (owner-only)
- [x] 4.3 Implement publish/unpublish lifecycle
- [x] 4.4 Implement admin course list/delete

## 5. Course Structure

- [x] 5.1 Implement module CRUD (owner-only, ordered)
- [x] 5.2 Implement lesson CRUD with markdown content (owner-only, ordered)

## 6. Enrollment

- [x] 6.1 Implement enroll/withdraw actions with duplicate prevention
- [x] 6.2 Implement "My Courses" page for Students

## 7. Progress Tracking

- [x] 7.1 Implement lesson completion mark/unmark (enrolled only)
- [x] 7.2 Implement per-course progress percentage display

## 8. UI Polish & Verification

- [x] 8.1 Shared layout and role-adaptive navigation
- [x] 8.2 Ensure public catalog and dashboards render correctly
- [x] 8.3 Run `dotnet build` and verify app starts
