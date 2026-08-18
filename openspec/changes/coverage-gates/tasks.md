# Coverage Gates — Tasks

## 1. Test Foundation

- [ ] 1.1 Create `tests/OpenLearning.UnitTests` (xUnit + Moq), add to solution
- [ ] 1.2 Unit tests for pure-logic services: ProgressService, ReviewService, CertificateService, SettlementService, CsvHelper, ProfileService validation branches
- [ ] 1.3 Wire coverlet OpenCover collection into the CI test step

## 2. Incremental Gate

- [ ] 2.1 Script/test computing PR diff lines → coverage report mapping (exclude tests, Migrations/*.Designer, obj)
- [ ] 2.2 Gate fails when new-line coverage < 80%; overall coverage reported only
- [ ] 2.3 Document local coverage command

## 3. Verification

- [ ] 3.1 Add a new untested method → gate red; add its test → gate green
- [ ] 3.2 Overall-coverage dip on legacy code does NOT fail CI
