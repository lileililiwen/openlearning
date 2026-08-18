# Architecture Enforcement — Tasks

## 1. Test Project

- [x] 1.1 Create `tests/OpenLearning.ArchitectureTests` (xUnit + ArchUnitNET), add to solution
- [x] 1.2 Encode rules: no module→Data reference, base-DbContext injection, acyclic pairs, Web composition root, cross-module misuse
- [x] 1.3 Document fixture update rule (new module → add fixture entry) in the test header

## 2. CI Wiring

- [x] 2.1 Ensure `dotnet test` in `ci-pipeline` runs the architecture tests — `dotnet test OpenLearning.sln` (the CI test step) now discovers the test project automatically

## 3. Verification

- [x] 3.1 Add a temporary illegal reference (module→Data) → architecture test fails; remove → passes — verified with an injected Ecommerce→Scorm edge (a module→Data project reference is impossible without a cycle since Data references every module): the graph rule failed with 1/5 and passed 5/5 after removal
