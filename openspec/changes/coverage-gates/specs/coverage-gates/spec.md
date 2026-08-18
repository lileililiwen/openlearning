## ADDED Requirements

### Requirement: Unit tests cover core logic

The system SHALL ship unit tests for core service logic that run in CI.

#### Scenario: Tests run in CI
- **WHEN** a push or PR triggers CI
- **THEN** the unit test suite runs and failures fail the pipeline

### Requirement: New code meets a coverage threshold

The system SHALL enforce an incremental coverage gate so that code added by a change is tested at or above a threshold, while overall (legacy) coverage is reported but not gated.

#### Scenario: New code under threshold
- **WHEN** a change adds executable lines whose coverage is below the threshold
- **THEN** the coverage gate fails

#### Scenario: New code meets threshold
- **WHEN** a change's new executable lines are covered at or above the threshold
- **THEN** the gate passes

#### Scenario: Legacy coverage not gating
- **WHEN** a change touches existing code but overall coverage is below the threshold
- **THEN** the pipeline does not fail on the legacy lines
