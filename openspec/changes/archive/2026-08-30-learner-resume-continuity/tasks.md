# Implementation Tasks

> **Implementation note (deviation from the original design):** the repo already has a
> per-enrollment resume store — the `LessonAccess` table, written by
> `ProgressService.RecordAccessAsync` (invoked from `Pages/Courses/Lessons/View.cshtml.cs`).
> Adding a duplicate `Enrollment.ResumeLessonId` column + `AddEnrollmentResume` migration would
> re-implement that store. Instead this change reuses `LessonAccess` and adds a resolver
> (`IResumeService` / `ResumeService` in `OpenLearning.Progress`) that returns the most recently
> viewed lesson for a single course, with first-lesson fallback and stale-target handling. No
> schema migration is required.

## 1. Resume resolution service (OpenLearning.Progress)

- [x] Add `IResumeService` with `RecordViewAsync(userId, courseId, lessonId)` and
      `GetResumeTargetAsync(userId, courseId)` to `OpenLearning.Progress`.
      `GetResumeTargetAsync` returns the most-recently-accessed lesson that still exists and
      belongs to the course, otherwise the first lesson (by `Module.OrderIndex`, `Lesson.OrderIndex`),
      otherwise `null` when the learner is not enrolled or the course has no lessons.
- [x] `RecordViewAsync` upserts the `LessonAccess` row and is a no-op for non-enrolled learners or
      lessons that do not belong to the course (so owner previews / anonymous views are not recorded).
- [x] Register `IResumeService` in `AddProgressModule()` (`ProgressModuleExtensions.cs`).
- [x] Unit tests `tests/OpenLearning.UnitTests/ResumeServiceTests.cs` (6): record→resume, first-lesson
      fallback, stale (deleted) target fallback, not-enrolled returns null, non-enrolled no row,
      foreign lesson no row.

## 2. Record the view

- [x] In `Pages/Courses/Lessons/View.cshtml.cs` `OnGetAsync`, the enrolled branch now calls
      `_resume.RecordViewAsync(userId, course.Id, id)` (replacing the direct
      `ProgressService.RecordAccessAsync` call). It is already inside the `userId is not null &&
      isEnrolled` guard, so it never throws into the page and never records owner/preview views.

## 3. Fix entry points

- [x] `Pages/Courses/Details.cshtml`: the "Continue learning" button now links to
      `/Courses/Lessons/View/{ResumeTargetLessonId}` (computed in `DetailsModel`); hidden when
      the course has no lessons.
- [x] `Pages/Dashboard/Index.cshtml` "My courses" card footer now links to the resume target
      (`EnrolledCourseItem.ResumeLessonId`), falling back to course details when none.
- [x] `Pages/MyCourses.cshtml` "Continue" button links to the resume target
      (`EnrolledCourse.ResumeLessonId`), falling back to course details.
- [x] Resume targets are passed from the page models via `ResumeTargetLessonId` /
      `EnrolledCourseItem.ResumeLessonId` / `EnrolledCourse.ResumeLessonId`, each populated from
      `IResumeService.GetResumeTargetAsync`.

## 4. Verification

- [x] Unit tests: `ResumeServiceTests` (6) pass.
- [x] `dotnet build OpenLearning.sln` → 0 warnings / 0 errors.
- [x] `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed (no new module; Progress already
      allowed to depend on CourseManagement + Enrollment, which the service uses).
- [x] `dotnet format --verify-no-changes` → clean.
