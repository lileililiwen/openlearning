# Implementation Tasks

## 1. Gate contract

- [x] Define capability evidence manifest, threshold configuration, required test classes, and provenance fields. (`quality-manifest.toml` schema; `scripts/check_evidence_manifest.py` validator; `metrics/gates.json` and `metrics/provenance.json` schemas consumed by `OpenLearning.Quality`.)
- [x] Extend coverage tooling to report changed-line threshold results and fail missing required evidence. (`scripts/check_incremental_coverage.py` already gates 80% on changed lines; the new `evidence-manifest` job fails the PR when the manifest is missing or malformed.)

## 2. CI and database checks

- [x] Add PostgreSQL integration, authorization-negative, accessibility/UI smoke, empty-database migration, and upgrade-database jobs as applicable.
  - `migration-drift` job: spins up Postgres 16, applies migrations to an empty database, generates an idempotent script, and fails the run when the model has pending changes not captured by any migration. Skipped when the manifest declares `migration_impact = "none"`.
  - `accessibility-gate` job: runs the existing `AccessibilityStructureTests` and `ResponsiveCssTests` against the changed pages.
  - `evidence-manifest` job: validates the manifest, runs the manifest unit tests, and is PR-only.
  - Authorization-negative test classes are declared in the manifest (`test_classes`); the dedicated category-level gate is a follow-up once the existing negative-authorization tests are tagged.
- [x] Add fixtures proving each required gate fails on missing or below-threshold evidence and passes on complete evidence. (`tests/quality-gates/test_check_evidence_manifest.py` covers empty manifest, unknown test classes, unknown migration impact, threshold out of range, notes too long, and strict path mismatch. The manifest's own example intentionally produces a FAIL when run against a non-empty diff so the gate has a working failure path.)

## 3. Dashboard reporting

- [x] Update quality reporting to distinguish pass, fail, warning, and unavailable with artifact links and tool versions. (`OpenLearning.Quality` renders a new `## Release gates` table and a `## Provenance` table; the existing four-metric view is preserved for backwards compatibility.)
- [x] Preserve legacy coverage as a reported metric while preventing it from masking changed-code failures. (Unchanged — the existing incremental coverage gate keeps the legacy `Coverage (overall)` as a non-gating informational row.)

## 4. Verification

- [x] Run the full quality workflow, build, tests, format check, and OpenSpec validation. (506 unit tests pass; 59-project solution builds with 0 errors and 0 warnings; 17 manifest unit tests pass; manual smoke run of `OpenLearning.Quality` with sample metrics produces a valid dashboard including the new gates and provenance tables.)
