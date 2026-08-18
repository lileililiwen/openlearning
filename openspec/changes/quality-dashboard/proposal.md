## Why

Quality gates exist in CI but there is no single view of quality over time. The quality plan's Phase 3 asks for a quality dashboard and periodic quality reports so maintainers can see trends (build health, coverage, bugs, vulnerabilities) instead of chasing individual failures.

## What Changes

- A quality dashboard: a generated page/report aggregating build status, test/coverage results, analyzer findings, Sonar metrics, and audit results.
- Periodic quality reports: a scheduled CI job (e.g. weekly) produces a markdown/HTML report artifact and posts a summary to the repo.
- Trend baseline: simple history of key metrics (build pass rate, coverage, bugs, vulnerabilities) so regressions are visible.

## Capabilities

### New Capabilities
- `quality-dashboard`: aggregated quality view and periodic reports.

### Modified Capabilities

- `ci-pipeline`: publishes metrics from each run to the dashboard data store.

## Impact

- New `docs/quality/` with a generated report (`README.md` + JSON metrics), or a small `OpenLearning.Quality` console that merges CI artifacts.
- CI jobs write metrics JSON (build, coverage, sonar, audit) to an artifact; a scheduled job renders the dashboard and posts the summary.
- Metrics source: coverlet reports, Sonar API, audit output, test results.
