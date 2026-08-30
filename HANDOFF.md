# HANDOFF.md — Change Status & Handoff

> Progress / status document. Reusable by another AI session or conversation to
> continue this work. **Last updated: 2026-08-30.** Branch: `main`.
>
> Most recent change: **ui-state-and-feedback-contract** — COMPLETE & ARCHIVED
> (`openspec/changes/archive/2026-08-30-ui-state-and-feedback-contract`).
> Previous changes `learner-resume-continuity`, `outcomes-mastery-operations`
> also COMPLETE & ARCHIVED.

## Latest change: UI State & Feedback Contract (ui-state-and-feedback-contract)

> **Status: COMPLETE & ARCHIVED** (`openspec/changes/archive/2026-08-30-ui-state-and-feedback-contract`).

### Goal
Give every learner/instructor page a shared, consistent language for loading / empty /
error / success / confirmation / disabled states, surface `TempData["Message"]` as a
global toast (rather than silently discarding it), require confirmation for destructive
actions, preserve input on failed posts, and make the high-traffic in-lesson actions
(mark-complete, save-note) work via progressive enhancement while remaining functional
without JavaScript.

### What is DONE
- **Shared state partials** in `Pages/Shared/`: `_StateLoading.cshtml` (`role="status"`
  `aria-busy`), `_StateEmpty.cshtml` (message + optional primary action), `_StateError.cshtml`
  (message + optional retry), and `_Toast.cshtml` (global feedback toast reading
  `TempData["Message"]` / `TempData["MessageType"]`). CSS hooks added to `site.css`.
- **Global toast**: `_Layout.cshtml` renders `<partial name="_Toast" />` once. The per-page
  `TempData["Message"]` alert blocks were removed from every learner/instructor/admin page
  so the global toast is the single renderer (no double-render). `site-chrome.js` auto-dismisses
  the toast after 5s and exposes a `showToast()` helper for progressive-enhancement responses.
- **Confirmation helper**: `OpenLearning.Web/TagHelpers/ConfirmTagHelper.cs` moves a
  `confirm="…"` attribute to `data-confirm` (anchors also become `role="button"` with their
  original href preserved in `data-confirm-href`); `site-chrome.js` intercepts
  `form[data-confirm]` submit and `a[data-confirm]` navigation via `window.confirm`.
  Applied to `Details.cshtml` (Unpublish/Publish + Delete), `Notifications/Index.cshtml`
  (Mark all read), and `MyCourses.cshtml` (Withdraw) — replacing inline `onsubmit=confirm(...)`.
- **Input preservation**: `Courses/Lessons/View.cshtml` note textarea uses `asp-for="NoteBody"`
  and `OnPostSaveNoteAsync` re-validates `ModelState`, returning the partial with the entered
  text on failure; `Study/Index.cshtml` check-in note uses `asp-for="CheckInNote"`.
- **Progressive enhancement**: `site-chrome.js` `postWithFeedback` intercepts `form[data-pe]`,
  POSTs via `fetch` with `X-Requested-With: XMLHttpRequest`, swaps the `data-pe-target` region,
  and shows a toast; the original `<form method="post">` is retained for the no-JS fallback.
  `Courses/Lessons/View.cshtml.cs` returns `PartialView("_LessonActions", this)` for AJAX
  requests (mark-complete / uncomplete / save-note) with `X-Toast-Message` / `X-Toast-Type`
  response headers. The `_LessonActions.cshtml` partial is shared between the full page and the
  PE swap.

### Verification (green)
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (see SDK note below).
- `dotnet format --verify-no-changes` → clean for the changed projects.
- `ConfirmTagHelperTests` (3) green: form→`data-confirm`+`aria-label`, anchor href
  preservation + `role="button"`, and empty-confirm no-op.

### Key decisions / gotchas
- **SDK**: this repo pins .NET 8 (`global.json` `8.0.4xx`) but the default `dotnet` on this
  machine resolves to the 10.0 SDK, whose Razor generator is stricter and fails to compile
  many pre-existing inline-code views. Build/format/`dotnet test` MUST be run with the 8.0 SDK
  (e.g. `global.json` pinned to `8.0.424`, `rollForward: latestPatch`, as used for this change).
  `TreatWarningsAsErrors=true` + Sonar + `EnforceCodeStyleInBuild=true` remain in force.
- **Single toast renderer**: rather than add a global toast on top of ~70 existing per-page
  `TempData["Message"]` alerts (which would double-render), the per-page blocks were removed so
  `_Toast` in `_Layout` is the only renderer. All setters of `TempData["Message"]` (171 in
  `Pages/*.cs`) now surface through it.
- **PE scope**: `withdraw` uses the `confirm` helper + standard postback (no-JS fallback
  preserved); full region-swap PE for the MyCourses card is a deferred incremental follow-up.
  The handler ajax path is covered by build + the tag-helper unit test; a TestServer-level PE
  test would require `LessonService.GetByIdAsync` to be virtual/mockable (currently it is not).
- **Naming**: private constants follow the repo's `_camelCase` prefix rule (`.editorconfig`
  `private_fields_should_be_camel_case`); the tag-helper attribute constant is
  `_confirmAttributeName`.

### Goal
Make every "Continue learning" / "Resume" entry point navigate to the learner's last-viewed
lesson (falling back to the first lesson) instead of reloading the course details page.

### What is DONE
- `OpenLearning.Progress`: `IResumeService` + `ResumeService` (`GetResumeTargetAsync`,
  `RecordViewAsync`) reusing the existing `LessonAccess` store. **No schema migration** — the
  original design's `Enrollment.ResumeLessonId` column + `AddEnrollmentResume` migration was
  deliberately skipped to avoid duplicating `LessonAccess` (rationale in `tasks.md`).
- Registration in `AddProgressModule()`; unit tests (6) in `tests/OpenLearning.UnitTests/ResumeServiceTests.cs`.
- Wiring: `Pages/Courses/Lessons/View.cshtml(.cs)` records the view; `Details.cshtml`
  "Continue learning", `Dashboard/Index.cshtml` "My courses" card, and `MyCourses.cshtml`
  "Continue" all link to the resume target (hidden / falling back to details when none).

### Verification (green)
- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~ResumeServiceTests` → 6 passed.
- `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed.
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors.
- `dotnet format --verify-no-changes` → clean.

### Key decisions / gotchas
- Reuse `LessonAccess` (one row per enrollment+lesson, written by
  `ProgressService.RecordAccessAsync`) rather than a new `ResumeLessonId` column. `RecordViewAsync`
  is a no-op for non-enrolled/foreign lessons, so owner previews and anonymous views never become
  resume points.
- `GetResumeTargetAsync` returns the most-recently-accessed lesson that still exists and belongs to
  the course; deleted/unpublished targets fall back to the first lesson (by `Module.OrderIndex`,
  `Lesson.OrderIndex`). Returns `null` only when the learner is not enrolled or the course has no
  lessons (the CTA is hidden in that case).
- Strict build (`TreatWarningsAsErrors=true` + Sonar + `EnforceCodeStyleInBuild=true`): block bodies
  for methods, alias `using` placed last, `dotnet format --verify-no-changes` must stay clean.

---

## Goal of this change
Connect declared course outcomes to graded activities, calculate auditable mastery
states, and surface explainable learner risk signals to authorized instructors and
administrators. Learner delivery reads only published course content; outcome drafts
and mastery never leak to unauthorized viewers. (See
`openspec/changes/archive/2026-08-30-outcomes-mastery-operations/{proposal,design,spec,tasks}.md`.)

## Status
**Fully implemented and archived.** All four task groups are complete:
- Module `src/OpenLearning.Outcomes/` (models, config, operations service, module extensions).
- EF migration `20260830054819_AddOutcomes` (+ model snapshot).
- Wiring into `ApplicationDbContext`, `Program.cs`, `OpenLearning.Web`, `OpenLearning.Data`,
  `OpenLearning.UnitTests`, and the architecture fixture.
- Unit tests (9) + PostgreSQL integration tests (4) + Web page authorization tests (5)
  + architecture tests (5).
- **UI:** instructor outcomes page (`Pages/Courses/Outcomes/Index`) with outcome/mapping
  management and a per-learner mastery + intervention matrix, owner + Admin gated, linked
  from the course Edit page; learner self-view is read-only (no owner check, no writes).

## What is DONE

- New module `src/OpenLearning.Outcomes/` (no cross-module deps; allowed deps = none).
  - `Models/OutcomesModels.cs` — `CourseOutcome`, `OutcomeActivityMapping`, `MasteryResult`,
    `MasteryState` (NotStarted / Developing / Mastered / NeedsReview).
  - `Configuration/OutcomesConfiguration.cs` — 3 `IEntityTypeConfiguration` classes
    (indexes on `CourseId`, `(CourseId,OwnerId)`, `(CourseId,OutcomeId)`, unique
    `(OutcomeId,ActivityId)`, unique `(CourseId,OutcomeId,LearnerId)`).
  - `Services/OutcomeOperationsService.cs` — `CreateOutcomeAsync`, `AddOutcomeActivityMappingAsync`,
    `GetOutcomesWithMappingsAsync`, `CalculateWithAsync`, `RecalculateAsync`, `EvaluateAsync`,
    `GetCurrentMasteryAsync`. Owner checks throw `UnauthorizedOutcomeOperationException`.
  - `OutcomesModuleExtensions.cs` — `AddOutcomesModule`.
  - Public contracts: `ActivityResult`, `IOutcomeActivitySource`, `InterventionSignal`,
    `OutcomeEvaluation`, `MasteryCalculation`, request/result types.
- EF migration `20260830054819_AddOutcomes` (tables `CourseOutcome`, `OutcomeActivityMapping`,
  `MasteryResult`) + updated `ApplicationDbContextModelSnapshot.cs`.
- Wiring: `ApplicationDbContext` config scan, csproj references (Data + Web + UnitTests),
  `Program.cs` calls `AddOutcomesModule()` and registers `DbOutcomeActivitySource` as the
  `IOutcomeActivitySource`; architecture fixture lists `OpenLearning.Outcomes` with no deps;
  solution file lists the new project.
- **Instructor UI** `src/OpenLearning.Web/Pages/Courses/Outcomes/Index.cshtml(.cs)`:
  add outcome (title/description/threshold), map an activity (type/id/weight) with validation
  feedback, and a learner matrix showing mastery badge + explainable signals; owner
  (`Course.InstructorId`) + Admin gated; reachable from the course Edit page
  (`Pages/Courses/Edit.cshtml` "成果与掌握度" link).
- **Web activity adapter** `DbOutcomeActivitySource` reads `QuizAttempt` scores for the course
  and adapts them to `ActivityResult`. Gradebook/exam analytics can be added via the same
  `IOutcomeActivitySource` interface without touching the module.
- Unit tests `tests/OpenLearning.UnitTests/Outcomes/OutcomeOperationsServiceTests.cs` (9, pass):
  deterministic calc, boundary threshold, idempotent recalculation (single current row),
  invalid mapping rejection (weight/duplicate/type/threshold), intervention rules
  (MissingWork, RepeatedLowAttempts, StaleProgress, UnmetOutcome), NeedsReview state,
  unauthorized actor, learner self-view (no owner check, no write).
- PostgreSQL integration tests `tests/OpenLearning.UnitTests/Outcomes/OutcomeOperationsServicePostgresTests.cs`
  (4, pass) against a dedicated `openlearning_integration_outcomes` database: relational
  persist, idempotent upsert, unauthorized exception, relational mapping validation.
- Web page authorization tests `tests/OpenLearning.UnitTests/Web/OutcomesPageTests.cs` (5, pass):
  owner instructor → page; non-owner instructor → Forbid; student → Forbid; owner create
  outcome → success; non-owner create → Forbid.

## Verification already run (green)

- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~Outcomes` → 19 passed
  (9 service unit + 4 PostgreSQL integration + 5 Web page).
- `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed.
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors.

## Key decisions / gotchas for the next session

- The module computes mastery from activity results supplied via `IOutcomeActivitySource`
  (adapter in Web). It deliberately stores only `ActivityId` + `ActivityType` on
  `OutcomeActivityMapping` and never navigates to Assessments/Exams entities (modular-monolith
  rule: Outcomes may not depend on those modules). To add a new activity source, implement
  `IOutcomeActivitySource` and register it in `Program.cs` — no module change required.
- Mastery is deterministic and versioned (`OutcomeOperationsService.CurrentCalculationVersion`).
  Recalculation is idempotent: the single current `MasteryResult` row (unique on
  `(CourseId,OutcomeId,LearnerId)`) is upserted, so repeated calls never create conflicting
  current results. Source inputs + calculation version are retained in `SourceJson` for
  explanation/audit.
- Intervention signals are computed on the fly (explainable, never change grades): MissingWork
  (mapping with no result), RepeatedLowAttempts (Attempts ≥ 3 and best fraction < 0.5),
  StaleProgress (last attempt older than 30 days), UnmetOutcome (attempted but below threshold).
  When a learner is Developing with a review-worthy signal, the state is promoted to NeedsReview.
- Role/tenant scoping: all writes require the actor to equal `CourseOutcome.OwnerId`; the UI
  enforces `Course.InstructorId == user` OR `Roles.Admin` and passes `Course.InstructorId` as the
  actor (so admins act as owner). `EvaluateAsync` and `GetCurrentMasteryAsync` are read-only and
  safe for learner self-view.
- Build is strict: `TreatWarningsAsErrors=true` + Sonar + `EnforceCodeStyleInBuild=true`. Use
  block bodies for methods, avoid unnecessary `!` null-forgiving operators (S8969), avoid nested
  ternaries in Razor (S3358 — use helper methods like `BadgeClass`/`StateLabel`), and keep
  static helpers static (S2325).
- PostgreSQL integration tests use a dedicated `openlearning_integration_outcomes` database and
  call `EnsureDeletedAsync()`+`EnsureCreatedAsync()` per test so unique indexes never collide with
  leftovers from prior runs. Do NOT reuse `openlearning_integration` (owned by the Authoring
  Postgres tests, whose `EnsureCreated` will not create missing tables on an existing database).

## Suggested next steps (checklist)

1. **Archive complete** — no further tasks remain for this change.
2. Wire `IOutcomeActivitySource` adapters for Assignment/Exam results (and optionally gradebook
   aggregates) so mastery covers all assessable activity types, not just quizzes.
3. Before pushing `main`, run the remaining test suites and `dotnet format` if required by CI, and
   apply the `AddOutcomes` migration to the target database (`dotnet ef database update`).
