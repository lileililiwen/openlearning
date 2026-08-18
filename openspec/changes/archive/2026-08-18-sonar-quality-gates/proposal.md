## Why

Static analysis in CI is one layer, but SonarQube/SonarCloud adds continuous inspection: bugs, code smells, duplication, and complexity with merge-request quality gates. The quality plan's Phase 2 makes Sonar an MR gate.

## What Changes

- Integrate SonarScanner for .NET into the CI pipeline.
- Configure SonarCloud (or self-hosted SonarQube) project settings; run analysis on every push/PR.
- Merge-request quality gates: new-code coverage, new bugs, new code smells, and duplication thresholds must pass before merge.

## Capabilities

### New Capabilities
- `sonar-quality-gates`: continuous static analysis with MR quality gates.

### Modified Capabilities

- `ci-pipeline`: adds a Sonar analysis step and gate to the existing workflow.

## Impact

- `Directory.Build.props` gains Sonar analyzer packages (`SonarAnalyzer.CSharp` already added in Phase 1) and project metadata (ProjectGuid/Name for the scanner).
- CI workflow gains `SonarCloud` steps (begin → build/test/coverage → end) producing a gate status.
- Coverage tooling (coverlet) emits XML for Sonar (ties to `coverage-gates`).
- Secrets: `SONAR_TOKEN` and organization configured as CI secrets.
