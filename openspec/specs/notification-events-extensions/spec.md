# notification-events-extensions Specification

## Purpose
TBD - created by archiving change notification-events-extensions. Update Purpose after archive.
## Requirements
### Requirement: Import completion notification

The system SHALL emit an `import.completed` notification to the importer when an async import job (questions, students, course outline, coupons) finishes, carrying success count, error count, and a download link to the error file when applicable.

#### Scenario: Question import completes

- **WHEN** an async question import finishes
- **THEN** the importer receives an `import.completed` notification with `successCount`, `errorCount`, and the error-file link (when `errorCount > 0`)

#### Scenario: Student import completes

- **WHEN** an async student import finishes
- **THEN** the importer receives an `import.completed` notification with the same fields

#### Scenario: No error file

- **WHEN** the import finishes with `errorCount = 0`
- **THEN** the notification does not include an error-file link

### Requirement: Import failure notification

The system SHALL emit an `import.failed` notification to the importer when an async import job crashes, carrying a short error summary.

#### Scenario: Crash during import

- **WHEN** an async import throws
- **THEN** the importer receives an `import.failed` notification with the exception message

#### Scenario: Validation failure is not a crash

- **WHEN** an import finishes with `Status = Failed` due to file validation (size / extension)
- **THEN** the user sees an inline error on the upload page and no notification is sent (the user is still in front of the form)

### Requirement: Export ready notification

The system SHALL emit an `export.ready` notification when an async export job (grade export, course outline export) finishes, carrying a download link and the retention expiry date.

#### Scenario: Grade export ready

- **WHEN** an async grade export job finishes
- **THEN** the exporter receives an `export.ready` notification with a download link and the file's expiry date

#### Scenario: Link expires after retention

- **WHEN** a user opens the download link after the retention period (default 7 days, per `async-io-jobs`)
- **THEN** the page returns 404

### Requirement: Export progress notification

The system SHALL emit an `export.progress` notification at 25%, 50%, and 75% completion for async export jobs whose expected duration exceeds 5 minutes.

#### Scenario: Long export

- **WHEN** a grade export job reaches 50% completion
- **THEN** the exporter receives an `export.progress` notification with the percentage

#### Scenario: Short export skipped

- **WHEN** a grade export job finishes in under 5 minutes
- **THEN** no progress notifications are sent

### Requirement: Bulk-imported account welcome

The system SHALL emit an `account.welcome` notification to each user created via the student bulk import (per `student-bulk-import`), carrying the display name, the courses they were enrolled in, and (when no password was supplied) a one-time reset link.

#### Scenario: Account created with password

- **WHEN** an admin-supplied password is used
- **THEN** the welcome notification does not include a reset link

#### Scenario: Account created without password

- **WHEN** no password is supplied
- **THEN** the welcome notification includes a one-time reset link

### Requirement: Bulk-granted enrollment notification

The system SHALL emit an `enrollment.granted-bulk` notification to each existing user who is enrolled via the student bulk import, listing the courses they were enrolled in.

#### Scenario: Existing user enrolled

- **WHEN** an existing user is enrolled via the bulk import
- **THEN** the user receives an `enrollment.granted-bulk` notification

#### Scenario: New user

- **WHEN** a new user is enrolled via `CreateAndEnroll`
- **THEN** the welcome notification is sent instead of `enrollment.granted-bulk`

