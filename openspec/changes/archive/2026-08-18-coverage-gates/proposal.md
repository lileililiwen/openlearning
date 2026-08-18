## Why

There are no unit tests at all. The quality plan's Phase 3 requires enhancing unit testing with incremental coverage gates: tests exist for new code and coverage does not regress, instead of a one-time all-or-nothing mandate that would block on legacy code.

## What Changes

- Introduce test projects: core service tests (unit, mocked DbContext/UserManager), plus the architecture tests from `architecture-enforcement`.
- Coverlet collects line coverage; CI reports a coverage summary.
- Incremental coverage gate: new/added lines in a PR must meet a coverage threshold (e.g. ≥ 80%), enforced by comparing the PR diff to the coverage report; overall coverage is reported but not gated (legacy code is not penalized).

## Capabilities

### New Capabilities
- `coverage-gates`: unit-test foundation with incremental coverage enforcement.

### Modified Capabilities

- `ci-pipeline`: test step collects and gates on coverage.

## Impact

- New `tests/OpenLearning.UnitTests` project(s) with xUnit + coverlet + Moq.
- `Directory.Build.props` or CI passes `CollectCoverage=true`, `CoverletOutputFormat=opencover`.
- An incremental-coverage check (script or test) compares the PR diff lines to the coverage report and fails below threshold.
- `sonar-quality-gates` consumes the same OpenCover XML.
