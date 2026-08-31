# Design: Lesson Sequencing & Navigation

## Decisions

- **Ordered projection**: add `GetOrderedLessonsAsync(int courseId)` returning modules → lessons
  in authored order (already stored as `Module.Order` + `Lesson.Order` per `course-structure` spec),
  annotated with each lesson's completion for the current learner. Add `GetNavigatorAsync(int courseId, int currentLessonId)`
  returning `PrevLessonId`/`NextLessonId` (null at ends) computed from that ordering across module boundaries.
- **No new module dependency**: the navigator reads via existing repositories/queries; it does not
  require `Lesson` navigation properties from other modules. Completion comes from `OpenLearning.Progress`.
- **Curriculum sidebar partial** `Pages/Shared/_CurriculumSidebar.cshtml`: renders every module and
  lesson with a completion check (✓ when completed), highlights the current lesson, and links each
  lesson to `/Courses/Lessons/View/{id}`. Replaces `View.cshtml:153-168` (current-module-only list).
- **Next/Prev**: plain `<a>` links (server-rendered, keyboard-accessible by default) shown in the
  lesson footer; `Next` hidden when `NextLessonId` is null. Progressive enhancement (auto-advance,
  prefetch) is out of scope here and handled later by `ui-state-and-feedback-contract`.
- **Complete & Next**: add page handler `OnPostCompleteAndNextAsync` to `View.cshtml.cs` that marks
  the lesson complete (reuse the existing `Complete` logic) then redirects to `NextLessonId` (or back
  to `Details` when none). Keep the existing standalone `Complete`/`Uncomplete` handlers.
- **Lesson→assessment link**: when the course has a quiz/assignment associated with the lesson's
  module (or course), show a "Open quiz / assignment" link. Use existing relationships
  (`Quizzes`/`Assignments` queried by `courseId`); no new schema. If the mapping is only course-level,
  link to the course's quiz/assignment index.
- **Dashboard "due" deep link**: change `Dashboard/Index.cshtml:33` target from `/MyCourses` to the
  assignment detail/list route (e.g. `/Courses/Assignments/Detail?...` or `/Courses/Assignments/Index?courseId=...`).
- **Calendar month nav**: add `month` (and optional `year`) route/query parameter to `Study/Index`;
  provide prev/next `<a>` (or form) controls; default to current month when absent.

## Failure handling

- Navigator returns nulls gracefully when a course has a single lesson or no lessons.
- `CompleteAndNext` with no next lesson redirects to course details (no 404).
- Calendar with no data for a month still renders the navigable grid (empty state handled by `ui-state-and-feedback-contract`).

## Verification

- Unit test: `GetNavigatorAsync` returns correct prev/next across module boundaries; null at ends.
- UI test: clicking "Next" moves to the next lesson across modules; "Complete & Next" marks complete and advances; sidebar shows completion checkmarks.
- UI test: dashboard "due" links to the assignment, not `/MyCourses`; calendar month nav changes rendered month.
- `dotnet format` + build zero warnings; OpenSpec validation passes.
