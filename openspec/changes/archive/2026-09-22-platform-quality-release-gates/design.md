# Design: Platform Quality Release Gates

## Decisions

- **Evidence manifest at the repo root.** PRs that touch `src/` or `tests/` declare their change in `quality-manifest.toml` (paths, incremental coverage threshold, applicable test classes, migration impact). The manifest is PR-only — on main pushes it carries the "no active change" shape and the validator skips, so the default state does not require a stale commit. The validator (`scripts/check_evidence_manifest.py`) is a pure-Python script with no third-party dependencies so it runs in any GitHub-hosted Python runtime.
- **Per-gate CI jobs instead of one mega-step.** `evidence-manifest`, `migration-drift`, and `accessibility-gate` are separate jobs that produce individual pass/fail signals in the PR checks list. Each gate has a dedicated `id` in the main `build` job so the dashboard can roll them up into one status without losing which one failed.
- **Migration drift via idempotent script.** The `migration-drift` job spins up Postgres 16 in CI, applies migrations to an empty database, then generates an idempotent script (`dotnet ef migrations script --idempotent`). A non-empty idempotent script means the model has changes that no migration captured, so the job fails. This is the smallest reliable way to detect "model changed, no migration added" without a baseline-comparison file that would fail on every legitimate migration.
- **Dashboard provenance.** `OpenLearning.Quality` reads two new files: `metrics/gates.json` (one entry per gate with pass/fail/unavailable) and `metrics/provenance.json` (commit, run id, workflow, event, ref, actor, runner, dotnet version). Missing gates show as `unavailable`, never as a synthetic pass — the `b0b6ee1 fix(ci): emit honest Sonar metrics only when scan ran` change extended the same pattern to Sonar and the new gates follow it.
- **Test classes as manifest values, not traits yet.** The manifest's `test_classes` array declares which categories the change exercises (unit, postgres, authorization_negative, accessibility, migration, ui_smoke). The CI gate verifies the array is non-empty and the values are recognized. A future iteration can add `[Trait("Category", "...")]` attributes and a category filter so the gate verifies the test class actually ran.

## What this change does not do

- It does not retrofit every archived capability with a manifest declaration — the manifest is forward-looking; old changes stay as they are.
- It does not lower the existing 80% incremental coverage threshold. The manifest validates that the declared threshold is in `[0, 1]` but does not change what the gate enforces.
- It does not add a global "all tests must pass" gate. Each gate is explicit and the dashboard shows which gate is in which state.

## Verification

- `tests/quality-gates/test_check_evidence_manifest.py` exercises the validator against empty / unknown / out-of-range / mismatched-diff / no-base-ref cases. 17 tests pass.
- `OpenLearning.Quality` was smoke-tested with sample `metrics/{build,coverage,audit,sonar,gates,provenance}.json` and produces a dashboard that includes the new `## Release gates` and `## Provenance` sections.
- The 506-test xUnit suite still passes and the 59-project solution still builds with 0 warnings.

## Out of scope

- Tagging existing tests with `[Trait("Category", ...)]` so the gate can prove the declared class actually ran. The manifest is the source of truth for now; trait-based enforcement is a follow-up.
- A separate "capability coverage floor" that requires a minimum overall coverage percentage per capability. The existing incremental gate is the only coverage gate.
- Replacing the existing `quality-dashboard` artifact with a richer HTML report.
