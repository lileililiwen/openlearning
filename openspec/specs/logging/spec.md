# logging Specification

## Purpose
TBD - created by archiving change logging. Update Purpose after archive.
## Requirements
### Requirement: Operations are logged

The system SHALL record significant operations with the actor, action, target, timestamp, and IP address.

#### Scenario: Record operation
- **WHEN** a user performs a logged mutation (e.g. publish a course, suspend a user)
- **THEN** an operation log entry is created with the actor and action

#### Scenario: View logs
- **WHEN** an Admin opens the operations log
- **THEN** entries are listed and filterable by action, actor, and date

### Requirement: Errors are logged

The system SHALL persist unhandled exceptions with request context.

#### Scenario: Error log
- **WHEN** an unhandled exception occurs
- **THEN** an error log entry with message, stack trace, path, and user is stored

### Requirement: Logs are retained for a bounded period

The system SHALL prune logs older than a configured retention period.

#### Scenario: Retention
- **WHEN** logs exceed the retention period
- **THEN** they are deleted so the tables stay bounded

