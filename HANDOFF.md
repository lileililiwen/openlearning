# HANDOFF.md — Change Status & Handoff

> Progress / status document. Reusable by another AI session or conversation to
> continue this work. **Last updated: 2026-08-30.** Branch: `main`.
>
> Most recent change: **localization-foundation** — COMPLETE & ARCHIVED
> (`openspec/changes/archive/2026-08-30-localization-foundation`).
> Previous changes `extensibility-integration-contracts`, `ui-state-and-feedback-contract`,
> `learner-resume-continuity`, `outcomes-mastery-operations` also COMPLETE & ARCHIVED.

## Latest change: Localization Foundation (localization-foundation)

> **Status: COMPLETE & ARCHIVED** (`openspec/changes/archive/2026-08-30-localization-foundation`).

### Goal
Establish the ASP.NET Core localization foundation and migrate learner-facing Chinese/English
literals into `.resx` resources (English default + Chinese), so `<html lang>` can be set
correctly and no page mixes languages.

### What is DONE
- **Localization module** `src/OpenLearning.Web/LocalizationModuleExtensions.cs` —
  `AddLocalizationFoundation()` registers `AddLocalization(ResourcesPath = "Resources")` and
  configures `RequestLocalizationOptions`: `en` default, `zh` supported, and the
  cookie / query-string / accept-language request-culture providers.
- **Wiring in `Program.cs`**: `builder.Services.AddLocalizationFoundation();` after
  `AddRazorPages()`, and `app.UseRequestLocalization();` after `UseStaticFiles()` (before
  `UseRouting()`). Added `using Microsoft.AspNetCore.Localization;` and `using OpenLearning.Web;`.
- **Shared chrome resources**: `SharedResources` marker class (intentionally empty;
  `[SuppressMessage("SonarAnalyzer","S2094")]`) + `Resources/SharedResources.zh.resx`.
  `_Layout.cshtml` injects `IStringLocalizer<SharedResources>`, sets
  `<html lang="@CultureInfo.CurrentUICulture.TwoLetterISOLanguageName">`, and replaces the
  hardcoded Chinese chrome (cart, notifications, profile, membership, become-instructor,
  sign-out, suspended-account alert, footer license) with `@SharedLoc["…"]`.
- **Migrated priority pages** (English key + Chinese value, named after the page **model
  class** so `IStringLocalizer<T>` resolves them):
  - `MyCourses.cshtml` → `Resources/Pages/MyCoursesModel.zh.resx` (incl. the mixed-language warning).
  - `Courses/Lessons/View.cshtml` → `Resources/Pages/Courses/Lessons/ViewModel.zh.resx`
    (empty state, captions, back-to-course, etc.).
  - `Courses/Details.cshtml` → `Resources/Pages/Courses/DetailsModel.zh.resx`.
  - `Courses/Edit.cshtml` → `Resources/Pages/Courses/EditModel.zh.resx` ("成果与掌握度", "版本管理", labels).
  - `_ViewImports.cshtml` adds `@using Microsoft.Extensions.Localization`.
  - `Dashboard/Index.cshtml` and `Dashboard/Teacher.cshtml` were audited and are already
    English-only, so they were left unchanged (deviation from the priority list — no mixed
    language problem existed).
- **Tests** (`tests/OpenLearning.UnitTests/Localization/LocalizationFoundationTests.cs`, 7
  facts, all green): en renders the English key, zh renders the Chinese value for each
  migrated model, an untranslated key falls back to English (`ResourceNotFound = true`), and
  `RequestLocalizationOptions` is registered with `en` default + `zh` supported + the three
  providers (resolved reflectively from the `IConfigureOptions<>` descriptor to avoid a hard
  shared-framework dependency in the test project).

### Verification (green)
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (8.0.424 SDK).
- `dotnet test tests/OpenLearning.UnitTests` → 481 passed (incl. the 7 localization tests).
- `dotnet format --verify-no-changes` → clean for the whole solution.

### Key decisions / gotchas
- **Resource naming matches the model class, not the `.cshtml` file**: `IStringLocalizer<T>`
  builds the resource base name from `T`'s full name, so `MyCourses.cshtml` (model
  `MyCoursesModel`) needs `MyCoursesModel.zh.resx`, not `MyCourses.zh.resx`. The SDK embeds
  culture-specific `.resx` as a `zh` satellite assembly (`bin/…/zh/OpenLearning.Web.resources.dll`).
- **`en` is the default / fallback**: there is no neutral `*.resx` for pages — English strings
  are the keys, so an untranslated key renders the English text and never throws.
- Build is strict: `TreatWarningsAsErrors=true` + Sonar + `EnforceCodeStyleInBuild=true`;
  `dotnet format --verify-no-changes` must stay clean.


## Suggested next steps (checklist)

1. **Archive complete** — `localization-foundation` archived; no further tasks remain for it.
2. **Next spec to implement next turn: `learner-accessibility-compliance`** — build on the
   localization foundation (esp. the `<html lang>` hook) to add learner-facing accessibility
   compliance. (This is the spec named as the follow-up to `localization-foundation`.)
3. Remaining active specs after that: `learner-experience-quality`,
   `lesson-sequencing-navigation`, `offline-sync-resilience`, `platform-quality-release-gates`,
   `responsive-design-system`.
4. Before pushing `main`, run the full test suites and `dotnet format` if required by CI,
   and apply the `AddIntegrations` migration to the target database (`dotnet ef database update`).

---

## Previously archived changes (summary)

### UI State & Feedback Contract (ui-state-and-feedback-contract) — COMPLETE & ARCHIVED
Shared loading/empty/error/toast partials, global `TempData["Message"]` toast (single
renderer, per-page blocks removed), `ConfirmTagHelper` for destructive actions, input
preservation on failed posts, and progressive-enhancement for mark-complete / save-note.
Verification green (build, `dotnet format`, `ConfirmTagHelperTests` ×3).

### Learner Resume Continuity (learner-resume-continuity) — COMPLETE & ARCHIVED
`IResumeService` + `ResumeService` reuse `LessonAccess` (no schema migration) so every
"Continue learning" / "Resume" entry point navigates to the last-viewed lesson. 6 service
unit tests + 5 architecture tests + build/format green.

### Outcomes & Mastery Operations (outcomes-mastery-operations) — COMPLETE & ARCHIVED
`OpenLearning.Outcomes` module (models, config, operations service, module extensions), EF
migration `AddOutcomes`, instructor outcomes UI with mastery + intervention matrix, and
`DbOutcomeActivitySource` adapter. 19 service/integration/web tests + 5 architecture tests,
build/format green. Next follow-up: wire `IOutcomeActivitySource` adapters for Assignment/
Exam results so mastery covers all assessable activity types.

### Extensibility & Integration Contracts (extensibility-integration-contracts) — COMPLETE & ARCHIVED
`OpenLearning.Integrations` module (no cross-module deps): `IIntegrationAdapter` contract +
Webhook/Lti/Scorm adapters, `IntegrationOperationsService` (idempotent delivery, bounded
retry → dead-letter, fails closed, secret redaction, tenant scoping), EF migration
`AddIntegrations`, admin UI, and 20 tests (12 service + 4 Postgres + 4 web) + 5 architecture
tests. Build/format green.
