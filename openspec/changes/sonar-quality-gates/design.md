# Sonar Quality Gates — Design

## Context

Sonar brings centralized, historical analysis plus PR quality gates beyond what the local analyzers give.

## Goals

- Every PR is analyzed by Sonar and the result appears as a check.
- A quality gate on new code blocks merges for bugs, smells, duplication, and low coverage.
- Historical trends are available in the Sonar project dashboard.

## Non-Goals

- No on-prem SonarQube administration (SonarCloud usage; a self-hosted deployment is a config swap).
- No custom quality-profile work in MVP (default .NET profile).
- No blocking on legacy/old-code metrics (gate applies to new code).

## Decisions

### D1: SonarCloud project + scanner
- Create a SonarCloud project bound to the repo.
- CI adds steps using `SonarSource/sonarcloud-scan-action`-style flow (or the official .NET scanner): `begin` with `sonar.cs.opencover.reportsPaths` (OpenCover XML from tests), `dotnet build`, `dotnet test` with coverlet collecting OpenCover, `end` with `org` and `token` from secrets.

### D2: Quality gate
Enable the SonarCloud **New Code** gate on `main`: e.g. `Coverage on New Code ≥ 80%`, `Bugs=0`, `Vulnerabilities=0`, `Code Smells=0`, `Duplicated Lines on New Code < 3%`. The MR check fails until the gate passes. Old code is measured but not gating.

### D3: Failure behavior
The CI step `end` fails the pipeline when the gate is red (a `qualitygate.status == ERROR` check). The Sonar check appears on the PR alongside the build check; `branch-protection` adds it as a required check once live.

## Risks / Trade-offs

- **Coverage gate friction** → Strict new-code coverage on greenfield modules is achievable; legacy code is excluded by the new-code scope.
- **Token hygiene** → `SONAR_TOKEN` stored as a CI secret, never in the repo.

## Migration Plan

No schema change. CI workflow edits + SonarCloud project config + secrets.

## Open Questions

- SonarCloud vs self-hosted SonarQube → default SonarCloud (no infra); config point documented.
