# Proposal: Platform Quality Release Gates

## Why

The quality dashboard reports broad success while documented overall coverage is only 1.4%. Existing build and coverage specifications do not fully require PostgreSQL behavior, authorization negatives, accessibility, UI smoke paths, or migration drift checks, and a passing dashboard today can hide "we have no evidence this was ever tested" behind a green color. Every release should answer "what gates ran, what passed, what was unavailable, and which commit produced these numbers" — and the answer should be machine-checkable, not inferred from a screenshot.

## What Changes

- A capability evidence manifest at the repo root (`quality-manifest.toml`) that PRs touching `src/` or `tests/` must populate with their changed paths, incremental coverage threshold, test classes, and migration impact. The validator (`scripts/check_evidence_manifest.py`) is pure Python, dependency-free, and unit-tested.
- Three new CI jobs in `.github/workflows/ci.yml`:
  - `evidence-manifest` (PR-only): validates the manifest and runs the manifest unit tests.
  - `migration-drift` (skipped when `migration_impact = "none"`): spins up Postgres 16, applies migrations to an empty database, and fails the run if the model has pending changes not captured by any migration.
  - `accessibility-gate`: runs `AccessibilityStructureTests` and `ResponsiveCssTests`.
- A new `metrics/gates.json` and `metrics/provenance.json` schema consumed by `OpenLearning.Quality` so the dashboard distinguishes `pass` / `fail` / `unavailable` per gate and records commit, run id, workflow, event, ref, actor, runner, and dotnet version.
- New `tests/quality-gates/test_check_evidence_manifest.py` (17 tests) covering empty manifests, unknown test classes, unknown migration impact, threshold out of range, notes too long, strict path mismatch, and the PR-only / main-branch behaviors.
- The existing `quality-report.yml` weekly workflow is updated to emit the same `gates.json` / `provenance.json` so the trend view keeps the new sections.

## Non-goals

- Requiring a single global coverage percentage immediately. The incremental coverage gate stays at 80% on changed lines.
- Replacing the existing CI provider or the existing `OpenLearning.Quality` dashboard generator.
- Treating static-analysis scores as proof of business correctness.
- Tagging existing tests with `[Trait("Category", ...)]` and adding a category filter. The manifest declares the test class; trait-based enforcement is a follow-up.

## Impact

- Forward-looking only: archived capabilities do not need a retroactive manifest.
- One new file at the repo root (`quality-manifest.toml`) ships in the "no active change" state so the main branch stays green.
- New CI jobs increase the PR checks list from 1 to 4 (build + evidence-manifest + migration-drift + accessibility-gate).
- The dashboard adds a `## Release gates` table and a `## Provenance` table; the existing four-metric view is preserved for backwards compatibility.
- The 506-test xUnit suite still passes and the 59-project solution still builds with 0 warnings.
