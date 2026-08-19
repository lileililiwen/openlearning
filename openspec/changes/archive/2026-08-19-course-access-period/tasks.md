## 1. Model & Migration

- [x] 1.1 Add `Enrollment.AccessExpiresAt`, `Enrollment.RevokedAt`, `Enrollment.RevokedReason` (all nullable) to `src/OpenLearning.Enrollment/Models/Enrollment.cs`
- [x] 1.2 Add `Course.DefaultAccessDays` (nullable int) to `src/OpenLearning.CourseManagement/Models/Course.cs`
- [x] 1.3 Update `EnrollmentConfiguration` and `CourseConfiguration` with the new columns
- [x] 1.4 EF migration `AddAccessPeriod` via `dotnet ef migrations add AddAccessPeriod --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 1.5 Confirm migration applies cleanly on dev DB; existing rows have `null` values

## 2. Service Layer

- [x] 2.1 Add `EnrollmentService.SetExpiryAsync(enrollmentId, expiresAt, actorId)` with ownership check (Instructor of the course OR Admin/Finance)
- [x] 2.2 Add `EnrollmentService.RevokeAsync(enrollmentId, reason, actorId)`; reject if already revoked
- [x] 2.3 Add `EnrollmentService.IsAccessExpiredAsync(enrollment)` evaluating `AccessExpiresAt + graceDays`
- [x] 2.4 Add `EnrollmentService.ListExpiredPastGraceAsync()` for the scheduled job
- [x] 2.5 Update `EnrollmentService.EnrollAsync` to seed `AccessExpiresAt` from `Course.DefaultAccessDays` when set, and from `min(Membership.ExpiresAt, course default)` for membership-granted enrollments
- [x] 2.6 Update duplicate-enrollment guard to treat `Revoked` rows as not blocking a new enrollment

## 3. Course Edit Page

- [x] 3.1 Update `Pages/Courses/Edit.cshtml(.cs)` to accept `DefaultAccessDays` (nullable integer)
- [x] 3.2 Validate `DefaultAccessDays > 0` when provided

## 4. Enrollment Edit Page

- [x] 4.1 New `Pages/Courses/Enrollments/Edit.cshtml(.cs)` for instructors — pick a student, set `AccessExpiresAt`, save
- [x] 4.2 New `Pages/Admin/Enrollments.cshtml(.cs)` for Admin/Finance — list with filter by course/student; revoke action
- [x] 4.3 Both pages record an operation-log entry on each mutation

## 5. Learner UX

- [x] 5.1 Update `Pages/MyCourses.cshtml(.cs)` to render a banner listing enrollments inside the grace period or expired-but-not-revoked, with a "Renew" CTA
- [x] 5.2 Update lesson/quiz/assignment write handlers (`LessonComplete`, `QuizTake`, `AssignmentSubmit`) to deny writes when `IsAccessExpiredAsync` returns true; show a renewal prompt

## 6. Scheduled Job Hook

- [x] 6.1 Register an `IJob` named `enrollment.expiry.revoke` that calls `EnrollmentService.ListExpiredPastGraceAsync` and revokes each, sending the `enrollment.expired` notification (delegated to `scheduled-business-jobs` for cron registration; this change ships the `IJob` class)
- [x] 6.2 Register an `IJob` named `enrollment.expiry.notify-soon` that sends `enrollment.expiring-soon` notifications for enrollments expiring within 7 days (delegated to `scheduled-business-jobs`)

## 7. System Config

- [x] 7.1 Add `enrollment.expiry.graceDays` to the system-config defaults (value = 3)
- [x] 7.2 Verify the existing `Pages/Admin/System.cshtml` UI exposes the new parameter for editing

## 8. Build & Verify

- [x] 8.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 8.2 HTTP smoke tests:
  - Create a paid course, set `DefaultAccessDays = 1`, enroll a student; verify `AccessExpiresAt ≈ now + 1 day`
  - Force `AccessExpiresAt = UtcNow - 4 days`; trigger the `enrollment.expiry.revoke` job via admin Jobs → Run-now; verify `RevokedAt` is set and the learner gets a notification
  - Verify the learner can still view history (lessons, attempts, certificates) but is blocked from `LessonComplete` and `QuizTake`
  - Re-enroll the learner; verify a new row is created and progress is preserved
- [x] 8.3 Verify a free course with `DefaultAccessDays = null` keeps the existing behaviour (no expiry)