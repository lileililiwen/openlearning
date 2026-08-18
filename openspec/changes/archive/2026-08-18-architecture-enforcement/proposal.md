## Why

The modular-monolith pattern is enforced only by discipline and the compiler's own reference graph. Nothing prevents a module from depending on `OpenLearning.Data` or from a future cross-module cycle slipping in. The quality plan's Phase 3 introduces ArchUnitNET to validate architecture in CI.

## What Changes

- Add a test project using ArchUnitNET that encodes the architecture rules from `Agents.md`.
- Rules verified in CI: modules never reference `OpenLearning.Data`; services use the base `DbContext`; the module reference graph is acyclic; the Web project is the only composition root referencing all modules; no `OpenLearning.*` type is used from an unrelated layer.
- CI runs the architecture tests on every PR.

## Capabilities

### New Capabilities
- `architecture-enforcement`: ArchUnitNET architecture tests validating the module boundaries.

### Modified Capabilities

- `lms-core`: architecture rules are machine-checked, not just documented.

## Impact

- New `tests/OpenLearning.ArchitectureTests` project with ArchUnitNET.
- Architecture rules extracted into a test fixture encoding `Agents.md` §2.
- `ci-pipeline` runs `dotnet test` over the architecture test project.

## Dependencies

- Requires a test project (this change introduces the first one; `coverage-gates` builds on it).
