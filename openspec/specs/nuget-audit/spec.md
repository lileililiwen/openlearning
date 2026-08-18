# nuget-audit Specification

## Purpose
TBD - created by archiving change nuget-audit. Update Purpose after archive.
## Requirements
### Requirement: Dependency vulnerabilities are scanned

The system SHALL scan direct and transitive NuGet packages for known vulnerabilities during build and CI, failing on high/critical findings.

#### Scenario: High-severity vulnerability
- **WHEN** a direct or transitive package has a known high/critical vulnerability
- **THEN** the restore or CI audit reports it and the pipeline fails

#### Scenario: Clean dependencies
- **WHEN** no package has a known high/critical vulnerability
- **THEN** the audit passes and does not block the pipeline

### Requirement: Accepted risks are explicit

The system SHALL allow a documented, reviewed exception for vulnerabilities that cannot be fixed by upgrading, and SHALL NOT silently ignore findings.

#### Scenario: Suppressed advisory
- **WHEN** a vulnerability is accepted
- **THEN** it is recorded in the explicit suppress list with a rationale and the rest of the audit still runs

