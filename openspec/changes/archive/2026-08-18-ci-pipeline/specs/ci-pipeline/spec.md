## ADDED Requirements

### Requirement: Every push and PR is verified by CI

The system SHALL run a CI pipeline on every push to `main` and every pull request that:
- checks formatting with `dotnet format --verify-no-changes`,
- builds the solution with warnings-as-errors,
- runs the unit test suite.

#### Scenario: Format violation fails
- **WHEN** a change deviates from the configured formatting
- **THEN** CI fails with a format error before build/tests run

#### Scenario: Build warning fails
- **WHEN** a change introduces a compiler or analyzer warning
- **THEN** CI fails on the build step

#### Scenario: Test failure fails
- **WHEN** a unit test fails
- **THEN** CI reports the failing test and the pipeline is red

### Requirement: CI results gate merges

The system SHALL surface the CI pipeline result as a required check on pull requests.

#### Scenario: Required check
- **WHEN** a pull request is considered for merge
- **THEN** the CI status check must be passing for the merge to be allowed
