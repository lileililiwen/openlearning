# Coverage Gates — Tasks

## 1. Test Foundation

- [x] 1.1 Create `tests/OpenLearning.UnitTests` (xUnit + Moq), add to solution
- [x] 1.2 Unit tests for pure-logic services: ProgressService, ReviewService, CertificateService, SettlementService, CsvHelper, ProfileService validation branches — SettlementService does not exist yet (future settlement-cluster change); the other five are covered
- [x] 1.3 Wire coverlet OpenCover collection into the CI test step — `coverlet.collector` added to both test projects; CI test step collects OpenCover via `Coverlet.runsettings`

## 2. Incremental Gate

- [x] 2.1 Script/test computing PR diff lines → coverage report mapping (exclude tests, Migrations/*.Designer, obj) — `scripts/check_incremental_coverage.py`
- [x] 2.2 Gate fails when new-line coverage < 80%; overall coverage reported only
- [x] 2.3 Document local coverage command — CONTRIBUTING.md § Local coverage

## 3. Verification

- [x] 3.1 Add a new untested method → gate red; add its test → gate green — verified after commit (temp untested method made the gate fail; removal restored green)
- [x] 3.2 Overall-coverage dip on legacy code does NOT fail CI — the gate only scores executable lines added by the diff; untouched legacy files are never evaluated
