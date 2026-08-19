## ADDED Requirements

### Requirement: Assignment graded notification

The system SHALL emit an `assignment.graded` notification to the student when an instructor grades their submission, with the assignment title, score, and a link to the assignment detail.

#### Scenario: Grade triggers notification

- **WHEN** an Instructor grades a submission
- **THEN** the student receives an `assignment.graded` notification

#### Scenario: Re-grade does not re-notify

- **WHEN** the same instructor updates the score
- **THEN** no second notification is sent

### Requirement: Exam starting soon notification

The system SHALL emit an `exam.starting-soon` notification to every enrolled student who has not yet attempted the exam when the exam's `StartsAt` is within 30 minutes.

#### Scenario: T-30min reminder

- **WHEN** the `exam.reminder` job (`scheduled-business-jobs`) finds an exam starting in 30 min
- **THEN** each unenrolled-attempt student receives a notification

#### Scenario: Already-attempted students skipped

- **WHEN** the job iterates students
- **THEN** students with an existing attempt are skipped

### Requirement: Assignment due reminders

The system SHALL emit an `assignment.due-soon` notification when an assignment is due within 24 hours to each enrolled student who has not yet submitted.

#### Scenario: T-24h reminder

- **WHEN** the `assignment.due-reminder` job finds an assignment due in 24h
- **THEN** each non-submitting enrolled student receives a notification

The system SHALL emit an `assignment.due-missed` notification to a student who did not submit by the due date, fired when the job auto-closes the assignment.

#### Scenario: Due-missed

- **WHEN** the job auto-closes an assignment whose due date has passed
- **THEN** each enrolled student without a submission receives a `assignment.due-missed` notification

### Requirement: Class starting soon notification

The system SHALL emit a `class.starting-soon` notification to every member of a `ClassGroup` (per `class-groups`) when the class's `StartsAt` is within 30 minutes.

#### Scenario: T-30min class reminder

- **WHEN** the `class.start-reminder` job finds a class starting in 30 min
- **THEN** each enrolled student of that class receives a class-scoped notification

### Requirement: Enrollment expiring soon and expired notifications

The system SHALL emit an `enrollment.expiring-soon` notification when an enrollment is within 7 days of `AccessExpiresAt`, and an `enrollment.expired` notification when the enrollment is revoked (per `course-access-period`).

#### Scenario: T-7 days expiring

- **WHEN** the `enrollment.expiry.notify-soon` job runs
- **THEN** each learner whose enrollment expires within 7 days receives a notification

#### Scenario: Expired

- **WHEN** the `enrollment.expiry.revoke` job revokes an enrollment
- **THEN** the learner receives an `enrollment.expired` notification with a renewal CTA

### Requirement: Order expired notification

The system SHALL emit an `order.expired` notification to the buyer when the `order.expire-unpaid` job closes their unpaid order after 30 minutes (per `scheduled-business-jobs`).

#### Scenario: Close-unpaid

- **WHEN** the job cancels an unpaid order
- **THEN** the buyer receives an `order.expired` notification with a "retry purchase" link

### Requirement: Refund timeout-rejected notification

The system SHALL emit a `refund.timeout-rejected` notification to the student when the `refund.timeout-close` job auto-rejects their refund after 7 days.

#### Scenario: Refund timeout

- **WHEN** the job auto-rejects a pending refund
- **THEN** the student receives a notification explaining the timeout

### Requirement: Invoice lifecycle notifications

The system SHALL emit `invoice.issued`, `invoice.rejected`, `invoice.voided`, and `invoice.red-letter-issued` notifications per `invoice-management`.

#### Scenario: Invoice issued

- **WHEN** Finance issues an invoice request
- **THEN** the student receives an `invoice.issued` notification with a link to the printable view

#### Scenario: Invoice voided

- **WHEN** Finance voids an issued invoice
- **THEN** the student receives an `invoice.voided` notification with the reason

### Requirement: Notification can target a class

The system SHALL support a `ClassGroupId` foreign key on `Notification`; notifications with a non-null `ClassGroupId` are delivered only to enrolled students of that class.

#### Scenario: Class-scoped notification

- **WHEN** a class-scoped announcement is sent with `ClassGroupId`
- **THEN** only enrolled students of that class receive the notification

#### Scenario: Non-class notification

- **WHEN** a notification is sent without `ClassGroupId`
- **THEN** it follows the existing recipient-resolution rules

### Requirement: New event types have editable templates

The system SHALL seed a template for each new event type in the system-config store, and Admins SHALL be able to edit it via the existing `/Admin/System` notification-templates UI.

#### Scenario: Edit template

- **WHEN** an Admin edits the template for `assignment.graded`
- **THEN** subsequent notifications of that type use the new title and body

#### Scenario: Placeholders resolved

- **WHEN** the template contains placeholders like `{assignmentTitle}` or `{score}`
- **THEN** they are replaced at notification-creation time

### Requirement: New event types respect per-type preferences

The system SHALL respect the existing per-type notification preferences (from `account-settings`) for each new event type.

#### Scenario: Disable email for assignment.graded

- **WHEN** a user disables email for `assignment.graded`
- **THEN** no email is sent for that event; in-app delivery follows its own toggle

## ADDED Requirements

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