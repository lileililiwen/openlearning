# HANDOFF.md

> **Branch:** `main` | **Last updated:** 2026-09-22
>
> Most recent: **platform-quality-release-gates** — COMPLETE & ARCHIVED.
> Previous: `learner-experience-quality`, `offline-sync-resilience`, `lesson-sequencing-navigation`, `responsive-design-system`, `learner-accessibility-compliance`, `localization-foundation`, `extensibility-integration-contracts`, `ui-state-and-feedback-contract`, `learner-resume-continuity`, `outcomes-mastery-operations` — all ARCHIVED.

## Latest: Platform Quality Release Gates (platform-quality-release-gates)

**Status:** COMPLETE (`openspec/changes/archive/2026-09-22-platform-quality-release-gates`)

### What was done
- **Evidence manifest:** `quality-manifest.toml` at the repo root declares changed paths, incremental coverage threshold, test classes, and migration impact. Ships in the "no active change" shape so the main branch stays green.
- **Validator:** `scripts/check_evidence_manifest.py` — dependency-free Python, PR-only, fails on missing/empty/malformed manifests with strict path-diff check. 17 unit tests in `tests/quality-gates/`.
- **CI jobs:** Three new jobs in `.github/workflows/ci.yml`:
  - `evidence-manifest` (PR-only): validates the manifest and runs the unit tests.
  - `migration-drift`: spins up Postgres 16, applies migrations to an empty database, generates an idempotent script, fails on pending model changes. Skipped when the manifest declares `migration_impact = "none"`.
  - `accessibility-gate`: runs `AccessibilityStructureTests` and `ResponsiveCssTests`.
- **Dashboard:** `OpenLearning.Quality` reads new `metrics/gates.json` and `metrics/provenance.json`; renders `## Release gates` (per-gate pass/fail/unavailable) and `## Provenance` (commit, run id, workflow, event, ref, actor, runner, dotnet version) sections. Missing gates are "unavailable", never synthetic pass.
- **Weekly report:** `quality-report.yml` emits the same new schemas so the trend view keeps them.

### Files changed
- `quality-manifest.toml` (new)
- `scripts/check_evidence_manifest.py` (new)
- `scripts/fill_spec_purposes.py` (new)
- `tests/quality-gates/` (new, 17 tests)
- `.github/workflows/ci.yml`
- `.github/workflows/quality-report.yml`
- `src/OpenLearning.Quality/Program.cs`
- `openspec/specs/platform-quality-release-gates/spec.md` (new)
- 92 spec files: Purpose placeholders filled
- `HANDOFF.md`, `TASK.md`, `CONTRIBUTING.md` (this update)

### Verification
- `dotnet build` → 0 warnings, 0 errors (59 projects).
- 506 unit tests pass, 17 manifest tests pass.
- 0 TBD Purpose placeholders in `openspec/specs/`.

---

## Previously archived

| Change | Key deliverables |
|--------|-----------------|
| **learner-experience-quality** | Shared state partials (`_StateEmpty`, `_StateError`, `_StateLoading`), catalog error state, localized UI strings, responsive CSS tests, accessibility structure tests. |
| **offline-sync-resilience** | Batch sync endpoint with per-item results, `Lesson.ContentRevision` concurrency token, revision-staleness conflict detection, `MobileSyncService` shared write path, 7 mobile API tests. |
| **lesson-sequencing-navigation** | `LessonNavigatorService`, curriculum sidebar, next/prev nav, complete-and-next, assessment links, calendar month nav. |
| **responsive-design-system** | CSS custom properties, contrast fix, responsive tables, CDN SRI, breakpoint tokens. |
| **learner-accessibility-compliance** | `<html lang>` dynamic, progress bar ARIA, skip-to-content, sidebar landmarks, mobile menu a11y, label sweep, 7 a11y tests. |
| **localization-foundation** | ASP.NET Core localization module, `<html lang>` dynamic, `.resx` resources for SharedResources + 4 pages (en/zh), 7 localization tests. |
| **extensibility-integration-contracts** | `IIntegrationAdapter` contract, Webhook/Lti/Scorm adapters, `IntegrationOperationsService`, EF migration, admin UI, 20 tests. |
| **ui-state-and-feedback-contract** | Shared loading/empty/error/toast partials, `TempData` toast, `ConfirmTagHelper`, input preservation, progressive enhancement. |
| **learner-resume-continuity** | `IResumeService` + `ResumeService` reusing `LessonAccess`, 6 service + 5 architecture tests. |
| **outcomes-mastery-operations** | `OpenLearning.Outcomes` module, EF migration, instructor outcomes UI, 19 tests. |

---

## Next steps

1. **No active specs.** All OpenSpec changes are archived.
2. Before pushing: run tests, `dotnet format`, apply any pending EF migrations.
