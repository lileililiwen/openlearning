# Quality Dashboard — Tasks

## 1. Metrics Emission

- [ ] 1.1 CI steps write `build.json`, `coverage.json`, `sonar.json`, `audit.json` to a shared artifact directory
- [ ] 1.2 Add a run-info job that archives the metrics + history

## 2. Dashboard Generator

- [ ] 2.1 Create `src/OpenLearning.Quality` console rendering `docs/quality/README.md` from metrics + history
- [ ] 2.2 Add trend rows (build pass, coverage, bugs, vulnerabilities) for the last N runs

## 3. Scheduled Report

- [ ] 3.1 Weekly cron workflow producing a quality report and posting a summary (comment or issue)

## 4. Verification

- [ ] 4.1 Trigger CI → dashboard updates with the run's metrics; scheduled run posts a report with a trend table
