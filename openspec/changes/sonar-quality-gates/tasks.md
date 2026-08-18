# Sonar Quality Gates — Tasks

## 1. Project & Scanner Setup

- [ ] 1.1 Create the SonarCloud project and configure CI secrets (`SONAR_TOKEN`, org)
- [ ] 1.2 Add Sonar scanner begin/build/test(end with OpenCover reports) steps to the CI workflow
- [ ] 1.3 Ensure tests emit OpenCover XML (coverlet collector) consumed by the scanner

## 2. Quality Gate

- [ ] 2.1 Enable and tune the New Code gate (coverage ≥ 80%, 0 bugs/vulns/smells, duplication < 3%)
- [ ] 2.2 `end` step fails CI when the gate is red
- [ ] 2.3 Add Sonar check to required checks in `branch-protection`

## 3. Verification

- [ ] 3.1 Push a PR with a new-code defect/coverage gap → Sonar gate red and merge blocked; fix → gate green
