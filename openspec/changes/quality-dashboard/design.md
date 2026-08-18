# Quality Dashboard — Design

## Context

Quality data is scattered across CI logs, Sonar, and coverlet output. A dashboard aggregates it and a periodic report makes trends reviewable.

## Goals

- One aggregated view of the latest quality state.
- A scheduled report capturing trends over time.
- Minimal tooling: a console that reads CI-produced JSON and renders markdown/HTML.

## Non-Goals

- No hosted BI/analytics platform (files + CI artifacts).
- No real-time streaming dashboard (per-run snapshots).
- No alerting/notifications beyond the PR/issue posting.

## Decisions

### D1: Metrics emission
Each CI quality step writes a small JSON file to a shared artifact directory:
- `metrics/build.json` — pass/fail, warnings/errors, duration (parsed from build logs).
- `metrics/coverage.json` — overall and new-line coverage (from coverlet OpenCover).
- `metrics/sonar.json` — latest Sonar project metrics (from the Sonar API or gate output).
- `metrics/audit.json` — vulnerability count/level (from `nuget-audit`).

A `RunInfo` job collects them and stores `docs/quality/history/<date>.json` (committed on `gh-pages`-style branch or as a workflow artifact; decision: commit to a `quality` branch artifact — MVP: workflow artifact, not committed).

### D2: Dashboard generator
`src/OpenLearning.Quality` (small console, run in CI) reads the latest + history JSON and renders:
- `docs/quality/README.md` — status table: build, coverage (overall/new), bugs, vulnerabilities, duplication.
- Trend rows for the last N runs.

### D3: Scheduled report
A weekly cron workflow re-runs analysis steps (or reads the stored history) and posts a summary comment on the default branch (or opens an issue) titled "Quality report — <date>". The report includes the trend table and any regressions (e.g. coverage dropped, a new vulnerability).

## Risks / Trade-offs

- **Data freshness** → History accumulates only when CI runs; a weekly scheduled run refreshes it.
- **Metric parsing brittleness** → Parsers tolerate missing files (defaults) so a single missing step doesn't break the dashboard.

## Migration Plan

No schema change. New console project + workflow jobs + `docs/quality/`.

## Open Questions

- GitHub Pages hosting for the dashboard → deferred; a markdown report suffices for MVP.
