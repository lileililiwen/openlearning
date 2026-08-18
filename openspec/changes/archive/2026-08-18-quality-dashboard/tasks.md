# Quality Dashboard — Tasks

## 1. Metrics Emission

- [x] 1.1 CI steps write `build.json`, `coverage.json`, `sonar.json`, `audit.json` to a shared artifact directory — the `Emit quality metrics` step in ci.yml writes them from the build/audit outcomes and the OpenCover reports
- [x] 1.2 Add a run-info job that archives the metrics + history — the dashboard render + artifact upload steps archive `docs/quality/` per run (history accumulates in `docs/quality/history/`, gitignored)

## 2. Dashboard Generator

- [x] 2.1 Create `src/OpenLearning.Quality` console rendering `docs/quality/README.md` from metrics + history
- [x] 2.2 Add trend rows (build pass, coverage, bugs, vulnerabilities) for the last N runs

## 3. Scheduled Report

- [x] 3.1 Weekly cron workflow producing a quality report and posting a summary (comment or issue) — `.github/workflows/quality-report.yml` (weekly cron + manual dispatch); posts a dated summary issue via `gh` (host-dependent) and always uploads the dashboard artifact

## 4. Verification

- [x] 4.1 Trigger CI → dashboard updates with the run's metrics; scheduled run posts a report with a trend table — verified locally: the console merged real metrics (build pass, coverage 1.4%, zero advisories) into `docs/quality/README.md` with a trend table and history entry; the scheduled posting requires a hosted repo to exercise end-to-end
