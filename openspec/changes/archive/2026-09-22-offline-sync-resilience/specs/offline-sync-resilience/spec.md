# Offline Sync and Resilience Specification

## ADDED Requirements

### Requirement: Entitled published lessons can be cached

The system SHALL permit an authenticated learner to cache entitled published lesson content with its published revision and expiry metadata, and MUST NOT cache unpublished or unauthorized content.

#### Scenario: Learner caches entitled lesson

- **WHEN** an entitled learner requests a published lesson for offline use
- **THEN** the lesson and revision metadata are stored locally for the configured period

#### Scenario: Learner requests unauthorized lesson

- **WHEN** a learner requests content outside enrollment, access period, or tenant scope
- **THEN** the request is denied and no content is cached

### Requirement: Progress synchronization is idempotent

The system SHALL accept progress events with client idempotency keys and MUST return the same effective outcome for duplicate delivery.

#### Scenario: Sync retries after timeout

- **WHEN** the client resends an event after an unknown response
- **THEN** the server marks the duplicate without applying completion twice

#### Scenario: Batch partially fails

- **WHEN** a batch contains valid and invalid events
- **THEN** each item receives an accepted, duplicate, rejected, or conflict result and valid items are retained

### Requirement: Offline conflicts are visible

The system SHALL retain contradictory or stale progress events as conflicts and the learner UI SHALL show pending or failed synchronization with a retry action.

#### Scenario: Stale revision event

- **WHEN** a queued event references content older than the current published revision
- **THEN** the event is evaluated against the defined compatibility rule and otherwise retained as a visible conflict
