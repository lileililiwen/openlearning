# TASK.md — Current Work & Status

> Companion to `HANDOFF.md`. Tracks the in-flight / recently completed spec and the next
> item in the pipeline. **Last updated: 2026-09-22.**

## Just completed

- **`platform-quality-release-gates`** — COMPLETE & ARCHIVED
  (`openspec/changes/archive/2026-09-22-platform-quality-release-gates`).
  - Capability evidence manifest (`quality-manifest.toml`) validated by
    `scripts/check_evidence_manifest.py` (PR-only, 17 unit tests).
  - Three new CI jobs: `evidence-manifest`, `migration-drift` (Postgres 16 +
    idempotent script), `accessibility-gate`.
  - Dashboard adds `## Release gates` and `## Provenance` tables from
    `metrics/gates.json` and `metrics/provenance.json`.
  - 92 spec files: TBD Purpose placeholders filled.
  - 506 unit tests + 17 manifest tests pass; 59 projects build clean.

## Previously completed (archived)

- `learner-experience-quality` — shared state partials, catalog error state, responsive tests.
- `offline-sync-resilience` — batch sync, content-revision concurrency, 7 mobile API tests.
- `lesson-sequencing-navigation` — curriculum sidebar, next/prev nav, complete-and-next.
- `localization-foundation` — ASP.NET Core localization, en/zh resources, 7 tests.
- `learner-accessibility-compliance` — ARIA, skip-to-content, label sweep, 7 a11y tests.
- `responsive-design-system` — CSS tokens, contrast fix, responsive tables, CDN SRI.
- `extensibility-integration-contracts` — integration adapters, admin UI, 20 tests.
- `ui-state-and-feedback-contract` — shared partials, toasts, confirm helper.
- `learner-resume-continuity` — resume service, 11 tests.
- `outcomes-mastery-operations` — outcomes module, EF migration, 19 tests.

## Backlog

- No active specs. All OpenSpec changes are archived.

## Standing gates (must stay green)

- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (`TreatWarningsAsErrors=true`,
  Sonar, `EnforceCodeStyleInBuild=true`).
- `dotnet test tests/OpenLearning.UnitTests` → all green (506 tests).
- `python3 -m unittest discover -s tests/quality-gates -t .` → all green (17 tests).
- `dotnet format --verify-no-changes` → clean for the whole solution.
