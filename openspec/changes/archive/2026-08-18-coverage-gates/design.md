# Coverage Gates — Design

## Context

No tests exist; enforcing total coverage now would be impossible. Incremental gating measures what a PR adds without penalizing legacy code.

## Goals

- A real unit-test suite for core services.
- CI collects coverage per run.
- New code must meet a coverage threshold; overall coverage is reported but not gated.

## Non-Goals

- No 100%-coverage mandates.
- No test-count metrics (line coverage is the gate).
- No mutation testing.

## Decisions

### D1: Test projects
- `tests/OpenLearning.UnitTests`: xUnit, Moq, FluentAssertions (optional). Focus on pure logic services first: `ProgressService` (percent/session math), `ReviewService` (upsert + validation), `CertificateService` (issuance/idempotency), `SettlementService` (balance math), `CsvHelper` (escaping/formula neutralization), and auth `ProfileService` (validation branches). DB-touching services use `DbContext` against the in-memory provider or mocked `UserManager`; integration against PostgreSQL stays manual (HTTP smoke tests per `Agents.md`).
- `tests/OpenLearning.ArchitectureTests` (from `architecture-enforcement`) included in the coverage run.

### D2: Coverage collection
CI test step: `dotnet test --collect:"XPlat Code Coverage"` or coverlet console with `CoverletOutputFormat=opencover` per project, merged into a solution report. The report is uploaded as an artifact and consumed by Sonar (Phase 2).

### D3: Incremental gate
A small script/test (e.g. `tests/scripts/check-incremental-coverage.sh` or an xUnit custom check) computes the PR diff (from `git diff origin/main...HEAD`), maps changed lines to the coverage report (OpenCover), and fails when `covered/executable` on new lines < 80%. Excludes: test projects themselves, generated code (Migrations/*.Designer, obj). Overall coverage is printed but never gates.

## Risks / Trade-offs

- **Line-mapping complexity** → Diff→OpenCover line mapping is approximate; handled by a well-tested helper and generous rounding.
- **Gaming** → Threshold counts executable lines in the diff only; excluding tests/migrations is enforced in the script.

## Migration Plan

No schema change. New test projects + CI coverage step + script.

## Open Questions

- Should the gate live in CI only or also as a local command? MVP: CI only; a local `make coverage` convenience target documented.
