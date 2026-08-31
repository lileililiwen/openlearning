# HANDOFF.md

> **Branch:** `main` | **Last updated:** 2026-08-31
>
> Most recent: **responsive-design-system** — COMPLETE & ARCHIVED.
> Previous: `learner-accessibility-compliance`, `localization-foundation`, `extensibility-integration-contracts`, `ui-state-and-feedback-contract`, `learner-resume-continuity`, `outcomes-mastery-operations` — all ARCHIVED.

## Latest: Responsive Design System (responsive-design-system)

**Status:** COMPLETE & ARCHIVED (`openspec/changes/archive/2026-08-31-responsive-design-system`)

### What was done
- Added `:root` token block to `site.css` (colors, spacing, radius). Replaced all hardcoded hexes with `var(--...)`.
- Raised `.nav-item-link` contrast from 3.4:1 to 4.67:1 (AA pass) via `--color-nav-text: #b0b8c4`.
- Applied scoped responsive rule for all tables inside `.app-content` (overflow-x auto).
- Added `integrity` + `crossorigin` to Bootstrap 5.3.3 CDN `<link>`/`<script>` in `_Layout.cshtml`; added local JS fallback.
- Documented content-fit breakpoints (`--bp-narrow`, `--bp-medium`, `--bp-wide`) as named tokens.

### Verification
- `dotnet build` → 0 warnings, 0 errors.
- `dotnet format --verify-no-changes` → clean.

---

## Previously archived

| Change | Key deliverables |
|--------|-----------------|
| **learner-accessibility-compliance** | `<html lang>` dynamic, progress bar ARIA, skip-to-content, sidebar landmarks, mobile menu a11y, label sweep, 7 a11y tests. |
| **localization-foundation** | ASP.NET Core localization module, `<html lang>` dynamic, `.resx` resources for SharedResources + 4 pages (en/zh), 7 localization tests. |
| **extensibility-integration-contracts** | `IIntegrationAdapter` contract, Webhook/Lti/Scorm adapters, `IntegrationOperationsService`, EF migration, admin UI, 20 tests. |
| **ui-state-and-feedback-contract** | Shared loading/empty/error/toast partials, `TempData` toast, `ConfirmTagHelper`, input preservation, progressive enhancement. |
| **learner-resume-continuity** | `IResumeService` + `ResumeService` reusing `LessonAccess`, 6 service + 5 architecture tests. |
| **outcomes-mastery-operations** | `OpenLearning.Outcomes` module, EF migration, instructor outcomes UI, 19 tests. |

---

## Next steps

1. **Remaining active specs:** `learner-experience-quality`, `lesson-sequencing-navigation`, `offline-sync-resilience`, `platform-quality-release-gates`.
2. Before pushing: run tests, `dotnet format`, apply any pending EF migrations.
