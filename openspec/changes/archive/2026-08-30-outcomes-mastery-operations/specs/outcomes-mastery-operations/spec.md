# Outcomes and Mastery Operations Specification

## ADDED Requirements

### Requirement: Outcomes map to assessable activities

The system SHALL allow an authorized course owner to map course outcomes to assessable activities with weights and a mastery threshold, and MUST reject invalid or unweighted mappings.

#### Scenario: Owner maps an outcome

- **WHEN** an owner saves a valid outcome mapping
- **THEN** the mapping is stored with its weight and threshold and is available to calculation

#### Scenario: Invalid mapping

- **WHEN** an owner submits duplicate, negative, or incompletely weighted mappings
- **THEN** the system rejects the change with actionable validation errors

### Requirement: Mastery is deterministic and explainable

The system SHALL calculate mastery from recorded activity results using a versioned deterministic rule and SHALL retain the inputs and calculation version for explanation.

#### Scenario: Learner reaches threshold

- **WHEN** mapped activity results meet the configured threshold
- **THEN** the learner receives a Mastered result with source activities and calculation metadata

#### Scenario: Result is recalculated

- **WHEN** the same inputs are recalculated
- **THEN** the operation is idempotent and does not create conflicting current results

### Requirement: Intervention signals are role-scoped

The system SHALL identify explainable review signals for missing work, repeated low attempts, stale progress, or unmet outcomes, and SHALL expose them only within the viewer's authorized course and tenant scope.

#### Scenario: Instructor views at-risk learner

- **WHEN** an instructor opens operations for an owned course
- **THEN** matching signals show their rule, evidence, and last evaluated time

#### Scenario: Unauthorized operations access

- **WHEN** an instructor requests another owner's learner data or a learner requests another learner's data
- **THEN** access is denied and no private signal data is returned
