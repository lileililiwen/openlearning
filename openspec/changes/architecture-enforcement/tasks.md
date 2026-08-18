# Architecture Enforcement — Tasks

## 1. Test Project

- [ ] 1.1 Create `tests/OpenLearning.ArchitectureTests` (xUnit + ArchUnitNET), add to solution
- [ ] 1.2 Encode rules: no module→Data reference, base-DbContext injection, acyclic pairs, Web composition root, cross-module misuse
- [ ] 1.3 Document fixture update rule (new module → add fixture entry) in the test header

## 2. CI Wiring

- [ ] 2.1 Ensure `dotnet test` in `ci-pipeline` runs the architecture tests

## 3. Verification

- [ ] 3.1 Add a temporary illegal reference (module→Data) → architecture test fails; remove → passes
