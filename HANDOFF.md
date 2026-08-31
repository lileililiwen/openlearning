# HANDOFF.md

> **Branch:** `main` | **Last updated:** 2026-08-31
>
> Most recent: **learner-accessibility-compliance** — COMPLETE & ARCHIVED.
> Previous: `localization-foundation`, `extensibility-integration-contracts`, `ui-state-and-feedback-contract`, `learner-resume-continuity`, `outcomes-mastery-operations` — all ARCHIVED.

## Latest: Learner Accessibility Compliance (learner-accessibility-compliance)

**Status:** COMPLETE & ARCHIVED (`openspec/changes/archive/2026-08-31-learner-accessibility-compliance`)

### What was done
- `<html lang>` already dynamic from `CultureInfo.CurrentUICulture` (done in localization-foundation).
- 10 progress bars: added `aria-valuenow/min/max/aria-label` (inline or via `_ProgressBar.cshtml` partial).
- Skip-to-content link + `<main id="main">` + CSS reveal on focus.
- Sidebar `aria-label`, nav `aria-label="Primary"`, toggle `aria-expanded`/`aria-controls`.
- Mobile menu: `aria-expanded` toggle, focus trap, Esc-to-close, focus return.
- 12 unlabeled `<input>` elements: added `aria-label` (Lessons/View, Details, Qa, Surveys, Exams).
- 7 structural a11y tests (xunit + HtmlAgilityPack): progressbar ARIA, skip link, landmarks, labels.

### Verification
- `dotnet build` → 0 warnings, 0 errors.
- `dotnet test` → 488 passed (7 new a11y tests).
- `dotnet format --verify-no-changes` → clean.

---

## Previously archived

| Change | Key deliverables |
|--------|-----------------|
| **localization-foundation** | ASP.NET Core localization module, `<html lang>` dynamic, `.resx` resources for SharedResources + 4 pages (en/zh), 7 localization tests. |
| **extensibility-integration-contracts** | `IIntegrationAdapter` contract, Webhook/Lti/Scorm adapters, `IntegrationOperationsService`, EF migration, admin UI, 20 tests. |
| **ui-state-and-feedback-contract** | Shared loading/empty/error/toast partials, `TempData` toast, `ConfirmTagHelper`, input preservation, progressive enhancement. |
| **learner-resume-continuity** | `IResumeService` + `ResumeService` reusing `LessonAccess`, 6 service + 5 architecture tests. |
| **outcomes-mastery-operations** | `OpenLearning.Outcomes` module, EF migration, instructor outcomes UI, 19 tests. |

---

## Next steps

1. **Remaining active specs:** `learner-experience-quality`, `lesson-sequencing-navigation`, `offline-sync-resilience`, `platform-quality-release-gates`, `responsive-design-system`.
2. Before pushing: run tests, `dotnet format`, apply any pending EF migrations.
