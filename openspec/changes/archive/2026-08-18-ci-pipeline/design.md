# CI Pipeline — Design

## Context

No automated verification exists. CI is the safety net that makes the other quality gates meaningful (branch protection, analyzers, tests, coverage, sonar).

## Goals

- Every push and PR to `main` is verified automatically.
- The pipeline fails fast on format, build, or test failures.
- Results are visible to reviewers (status checks).

## Non-Goals

- No deployment/CD in MVP (pipeline is CI-only).
- No matrix builds beyond .NET 8 (single OS).
- No caching of restore beyond the standard GitHub Actions cache.

## Decisions

### D1: GitHub Actions workflow (`.github/workflows/ci.yml`)
Trigger: `push` and `pull_request` on `main`. Job `build` on `ubuntu-latest` with `actions/setup-dotnet@v4`, .NET 8.0.x. Steps:
1. Checkout (`fetch-depth: 0` so Sonar/diff tooling can work later).
2. Setup .NET 8.
3. `dotnet restore`.
4. `dotnet format OpenLearning.sln --verify-no-changes --no-restore` (fails on drift).
5. `dotnet build OpenLearning.sln -c Release --no-restore /warnaserror`.
6. `dotnet test OpenLearning.sln -c Release --no-build` (gates on tests once a test project exists; a `--no-build` pass is fine with zero test projects but a test project is added by `coverage-gates`).
7. Upload build logs on failure for diagnosis.

### D2: Status checks
The workflow's job must pass before merge — wired to `branch-protection` (required status check `build`). Failures are loud (annotations) and actionable.

### D3: Locale/line-endings
`.editorconfig` already normalizes end-of-line; the format check uses the repo defaults. `dotnet format` runs with the same SDK version pinned in the workflow.

## Risks / Trade-offs

- **Formatter version skew** → Pin the SDK in `global.json` (added in `editorconfig-and-analyzers` sweep) so local and CI format agree.
- **Slow first runs** → Restore/build are cached; format check is fast.

## Migration Plan

No schema change. One workflow file + README badge.

## Open Questions

- Should the pipeline also run `dotnet-audit` now? Deferred to `nuget-audit` (Phase 2).
