# Teacher Roster & Student Progress — Tasks

## 1. Query Helpers

- [x] 1.1 Add `GetRosterAsync(courseId, ownerId)` to `EnrollmentService` (projection with progress + last activity)
- [x] 1.2 Add per-student helpers: completion set (`ProgressService`), quiz attempts (`AttemptService`), SCORM records (`ScormRuntimeService`)

## 2. UI

- [x] 2.1 Roster page (`Pages/Courses/Roster/Index.cshtml`) with search-by-name
- [x] 2.2 Per-student progress page composing lessons/quizzes/SCORM/last access
- [x] 2.3 Withdraw-student confirmation flow (owner-only)
- [x] 2.4 Link roster from course edit page and teacher dashboard

## 3. Verification

- [x] 3.1 Run `dotnet build` and start the app
- [x] 3.2 Verify roster, per-student detail, and withdraw for the owner; non-owner denied
