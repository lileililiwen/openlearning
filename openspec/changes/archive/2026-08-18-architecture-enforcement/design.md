# Architecture Enforcement — Design

## Context

Agents.md §2 defines the modular-monolith rules but they are unenforced. ArchUnitNET turns those rules into executable tests.

## Goals

- Encode the module-dependency rules as ArchUnitNET tests.
- CI runs them so violations fail the pipeline.
- Rules are the single source of truth for the module graph.

## Non-Goals

- No runtime architecture checks (tests only).
- No dependency-viz generation in MVP.
- No enforcement of naming/styling (analyzers own that).

## Decisions

### D1: Test project + ArchUnitNET
`tests/OpenLearning.ArchitectureTests/OpenLearning.ArchitectureTests.csproj` (xUnit + `TngTech.ArchUnitNET` + `ArchUnitNET.xUnit`). A single `ModuleArchitectureTests.cs` builds the `Architecture` from the solution's compiled assemblies and asserts:

1. **No module references `OpenLearning.Data`** — every type in `OpenLearning.*` (except Data) `ShouldNotDependOnAny` types in `OpenLearning.Data`.
2. **Base DbContext injection** — services that depend on `Microsoft.EntityFrameworkCore.DbContext` must not reference the concrete `ApplicationDbContext`.
3. **Acyclic graph** — pairwise `ShouldNotDependOn` for any module pair that would form a cycle (encoded from the known graph; a future generic cycle check can be added).
4. **Web is the composition root** — every module assembly is referenced by `OpenLearning.Web` (or is itself a dependency of one that is).
5. **No cross-module navigation misuse** — types in module A do not depend on types in module B unless B is a declared dependency.

### D2: Rule source
Rules map 1:1 to `Agents.md` §2 bullets. When a new module is added, the test fixture gains an entry — the test failing is the signal to update the fixture (documented in the test file header).

### D3: CI wiring
`dotnet test` in `ci-pipeline` includes this project; a cycle or illegal reference fails the PR.

## Risks / Trade-offs

- **Fixture maintenance** → Each new module needs a fixture entry; the failing test + header comment make the update obvious.
- **ArchUnitNET learning curve** → Small, single-file fixture; the library is stable.

## Migration Plan

No schema change. New test project + CI test run.

## Open Questions

- Generic cycle detection vs explicit pair list → MVP: explicit pair assertions; a transitive-closure cycle check is a future enhancement.
