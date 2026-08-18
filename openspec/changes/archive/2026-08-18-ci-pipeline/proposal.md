## Why

There is no CI pipeline. Nothing automatically verifies that a change builds cleanly, is formatted, or keeps tests green. The quality plan's Phase 1 requires a pipeline that runs `dotnet format`, builds the solution, and executes unit tests on every push/PR.

## What Changes

- A CI pipeline (GitHub Actions workflow; adapter-friendly for other hosts) triggered on push and pull requests to `main`.
- Steps: checkout → setup .NET 8 → restore → `dotnet format --verify-no-changes` → `dotnet build` → `dotnet test`.
- The pipeline fails on any style deviation, build warning/error, or failing test.

## Capabilities

### New Capabilities
- `ci-pipeline`: automated format/build/test verification on every push and PR.

### Modified Capabilities

- `lms-core`: repository gains a CI workflow and documented status.

## Impact

- New `.github/workflows/ci.yml` (or equivalent) at the repo root.
- A unit-test project must exist for `dotnet test` to gate on (see `coverage-gates` for the test strategy); until then the pipeline builds and runs format checks.
- README gains a CI badge and contribution note.

## Dependencies

- Requires `editorconfig-and-analyzers` (format must be deterministic before CI enforces it).
- Requires at least one test project for the test step to be meaningful.
