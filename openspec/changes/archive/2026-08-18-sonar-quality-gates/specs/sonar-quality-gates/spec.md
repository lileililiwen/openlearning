## ADDED Requirements

### Requirement: Sonar analyzes every change

The system SHALL run Sonar analysis in CI for every push and pull request, reporting bugs, vulnerabilities, code smells, duplication, and coverage.

#### Scenario: PR analysis
- **WHEN** a pull request is opened or updated
- **THEN** Sonar analyzes the branch and reports its findings as a check

#### Scenario: Historical dashboard
- **WHEN** an admin opens the Sonar project
- **THEN** trends for bugs, smells, duplication, and coverage are available

### Requirement: Merge-request quality gate blocks low-quality new code

The system SHALL enforce a Sonar quality gate on new code so that new bugs, vulnerabilities, smells, excessive duplication, or low coverage block the merge.

#### Scenario: Gate failure
- **WHEN** new code violates the gate thresholds
- **THEN** the Sonar check fails and the merge is blocked

#### Scenario: Gate passes
- **WHEN** new code meets the gate thresholds
- **THEN** the Sonar check passes and does not block the merge

#### Scenario: Legacy code excluded
- **WHEN** a change touches existing code that does not meet current thresholds
- **THEN** only the newly added code is evaluated by the gate
