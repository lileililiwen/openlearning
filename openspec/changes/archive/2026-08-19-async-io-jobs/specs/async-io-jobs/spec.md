## ADDED Requirements

### Requirement: Async IO jobs are persisted

The system SHALL persist every async IO job as an `AsyncIOJob` row with a unique id, owner, kind, source file key, and status.

#### Scenario: Submit job

- **WHEN** an IO consumer calls `AsyncIOService.SubmitAsync(kind, file, ownerId)`
- **THEN** the file is stored, an `AsyncIOJob { Status = Pending }` is created, and an `IJob` is enqueued

#### Scenario: Job starts

- **WHEN** the `IJob` is picked up by `job-scheduler`
- **THEN** the `AsyncIOJob.Status` transitions to `Running` and `StartedAt` is set

### Requirement: Status transitions and idempotency

The system SHALL transition the job through `Pending → Running → Success / Failed`. Re-running a job SHALL be a no-op (per `job-scheduler`'s `IdempotencyKey`).

#### Scenario: Successful run

- **WHEN** the `IJob.ExecuteAsync` returns normally
- **THEN** the `AsyncIOJob.Status = Success`, `FinishedAt = UtcNow`, and a `ResultFileKey` (when applicable) is set

#### Scenario: Failed run

- **WHEN** the `IJob.ExecuteAsync` throws
- **THEN** the `AsyncIOJob.Status = Failed`, `FinishedAt = UtcNow`, and the error message is stored

### Requirement: Error file output

The system SHALL write a downloadable error file when an IO job completes with `ErrorRows > 0`; the file preserves the row index, the offending field, and the error message.

#### Scenario: Error file generated

- **WHEN** an import job completes with `ErrorRows > 0`
- **THEN** `AsyncIOJob.ErrorFileKey` is set and the user is notified with a download link

#### Scenario: No error file when zero errors

- **WHEN** an import job completes with `ErrorRows = 0`
- **THEN** no error file is produced

### Requirement: Owner-scoped visibility

The system SHALL allow a user to list and inspect only their own async IO jobs; Admin can list all jobs.

#### Scenario: User lists own jobs

- **WHEN** a user opens `/AsyncIO`
- **THEN** only jobs they own are listed

#### Scenario: Admin lists all jobs

- **WHEN** an Admin opens `/AsyncIO` with the `admin` filter
- **THEN** all jobs are listed newest first

#### Scenario: Cross-owner denied

- **WHEN** a user attempts to inspect another user's job
- **THEN** access is denied

### Requirement: File safety

The system SHALL accept only whitelisted extensions (per consumer) and SHALL enforce a per-job max size; rejections happen before storage.

#### Scenario: Wrong extension rejected

- **WHEN** an upload arrives with an extension not in the consumer's whitelist
- **THEN** the request is rejected with a 400 and no file is stored

#### Scenario: Oversize rejected

- **WHEN** an upload exceeds the consumer's `maxBytes`
- **THEN** the request is rejected with a 400 and no file is stored

### Requirement: File retention

The system SHALL retain result files and error files for a per-consumer retention period (default 7 days) and SHALL prune expired files via `scheduled-business-jobs`.

#### Scenario: Expired file removed

- **WHEN** a result file's age exceeds the retention period
- **THEN** the `scheduled-business-jobs` cleanup job deletes the file and sets `ResultFileKey = null`

#### Scenario: Expired link returns 404

- **WHEN** a user clicks a download link after the retention period
- **THEN** the page returns 404

### Requirement: Notification hooks

The system SHALL emit `import.completed`, `import.failed`, `export.ready`, `export.progress` notifications (defined in `notification-events-extensions`) at the appropriate transitions.

#### Scenario: Completion notification

- **WHEN** an import job finishes
- **THEN** the owner receives an `import.completed` notification with success / error counts and the error-file link (when applicable)

#### Scenario: Failure notification

- **WHEN** an import job throws
- **THEN** the owner receives an `import.failed` notification with the error summary

### Requirement: Job visibility on admin Jobs page

Every `IJob` registered by an IO consumer SHALL appear on the admin Jobs page (per `job-scheduler`).

#### Scenario: Job listed

- **WHEN** an Admin opens `/Admin/Jobs`
- **THEN** each IO consumer's job is listed with its key, cron (none for one-off async), last-run, and success rate

### Requirement: Audit log

The system SHALL write an `OperationLog` row per finished IO job recording the owner, kind, file key, success / error counts.

#### Scenario: Audit recorded

- **WHEN** an IO job finishes
- **THEN** an entry is visible in `/Admin/Logs/Operations`