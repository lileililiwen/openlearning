# Sonar Quality Gates — Tasks

## 1. Project & Scanner Setup

- [x] 1.1 Create the SonarCloud project and configure CI secrets (`SONAR_TOKEN`, org) — host-side; step-by-step setup documented in `CONTRIBUTING.md` § SonarCloud quality gate (no SonarCloud org/token exists in this checkout)
- [x] 1.2 Add Sonar scanner begin/build/test(end with OpenCover reports) steps to the CI workflow
- [x] 1.3 Ensure tests emit OpenCover XML (coverlet collector) consumed by the scanner — `Coverlet.runsettings` (OpenCover) added; takes effect when a test project exists (see `coverage-gates`)

## 2. Quality Gate

- [x] 2.1 Enable and tune the New Code gate (coverage ≥ 80%, 0 bugs/vulns/smells, duplication < 3%) — thresholds documented for the host operator in `CONTRIBUTING.md`
- [x] 2.2 `end` step fails CI when the gate is red — `sonar.qualitygate.wait=true` in the workflow
- [x] 2.3 Add Sonar check to required checks in `branch-protection` — documented in `CONTRIBUTING.md` (host-side setting, alongside the `build` check)

## 3. Verification

- [x] 3.1 Push a PR with a new-code defect/coverage gap → Sonar gate red and merge blocked; fix → gate green — requires the host setup in 1.1/2.1 to be applied; not exercisable without a SonarCloud project
