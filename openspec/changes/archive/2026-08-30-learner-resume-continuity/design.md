# Design: Learner Resume & Continuity

## Decisions

- **Storage**: add a `ResumeLessonId` (nullable) column to the `Enrollment` row, owned by
  `OpenLearning.Enrollment`. This is a plain scalar value (lesson id), so it introduces **no
  cross-module navigation dependency** on `CourseManagement`/`CourseStructure` (modular-monolith rule respected).
  Alternative considered: a separate `LearningResume` table keyed by `(UserId, CourseId)`. The
  enrollment-column approach is chosen because resume is naturally per-enrollment and avoids a new migration-heavy table.
- **Recording**: the lesson `View.cshtml` GET handler calls a new
  `IResumeService.RecordViewAsync(userId, courseId, lessonId)`. It updates `ResumeLessonId`
  only when the lesson belongs to an enrolled, published course (no-op for owners/preview).
- **Resolution**: `IResumeService.GetResumeTargetAsync(userId, courseId)` returns the resume
  lesson id if set and still valid (exists, belongs to course, course published), otherwise the
  **first lesson** of the course, otherwise `null` (caller falls back to course details).
- **Entry points fixed**:
  - `Courses/Details.cshtml:416` "Continue learning" → `/Courses/Lessons/View/{resumeLessonId}`.
  - Dashboard continue/resume card → same target (implementer locates the card in `Dashboard/Index.cshtml` / `Dashboard/Teacher.cshtml` and points it at the resume target instead of `/Courses/Details`).
- **No behavioral change for owners/admins** browsing a course; resume only applies to enrolled learners.

## Failure handling

- If `ResumeLessonId` points to a deleted/moved lesson, `GetResumeTargetAsync` ignores it and
  returns the first lesson (defensive, no exception to the learner).
- Recording a view is best-effort; a failure to persist must not break lesson rendering.

## Verification

- Unit test: `RecordViewAsync` sets `ResumeLessonId`; `GetResumeTargetAsync` returns it, falls
  back to first lesson when unset, and ignores a stale id.
- Integration/UI test: clicking "Continue learning" on a course the user has opened navigates to
  the last-viewed lesson, not the details page.
- `dotnet format` + build with zero warnings; OpenSpec validation passes (ADDED-only).
