# Implementation Tasks

## 1. Sequencing service

- [x] Add `GetOrderedLessonsAsync(int courseId)` returning `List<ModuleOutline>` (`ModuleTitle`, ordered `Lessons` with `Id`, `Title`, `IsCompleted`). Reuse existing ordering; join completion from `OpenLearning.Progress`.
- [x] Add `GetNavigatorAsync(int courseId, int currentLessonId)` returning `PrevLessonId`/`NextLessonId` (nullable) computed from the ordered projection across modules.
- [x] Unit tests for ordering, cross-module next, and null-at-ends.

## 2. Curriculum sidebar partial

- [x] Create `Pages/Shared/_CurriculumSidebar.cshtml(.cs)` taking the ordered outline + current lesson id; render modules/lessons, completion check, current highlight, links.
- [x] Replace `Pages/Courses/Lessons/View.cshtml:153-168` block with the partial. Populate `Model.Curriculum` in `View.cshtml.cs`.

## 3. Next / Prev / Complete & Next

- [x] In `View.cshtml.cs` add `Model.PrevLessonId`/`Model.NextLessonId` from `GetNavigatorAsync`.
- [x] Add next/prev `<a>` controls in the lesson footer (hidden when null).
- [x] Add `OnPostCompleteAndNextAsync`: reuse completion logic, then `return RedirectToPage("/Courses/Lessons/View", new { id = NextLessonId ?? courseId-fallback })`. When null, redirect to course details.
- [x] Keep existing `Complete`/`Uncomplete` handlers intact.

## 4. Lesson → assessment link

- [x] In `View.cshtml.cs`, query quizzes/assignments for the course (existing services) and expose `Model.AssessmentLink` (label + url) when present.
- [x] Render the link in `View.cshtml` near the completion area.

## 5. Dashboard & calendar fixes

- [x] `Dashboard/Index.cshtml:33`: change the "due" link target to the assignment detail/list route.
- [x] `Study/Index.cshtml(.cs)`: add `month`/`year` parameters, prev/next controls, default to current month, and render the selected month.

## 6. Verification

- [x] UI tests: Next crosses modules; Complete & Next advances; sidebar shows checks; dashboard due → assignment; calendar month nav works.
- [x] `dotnet format --verify-no-changes`, build zero warnings, OpenSpec validation passes.
