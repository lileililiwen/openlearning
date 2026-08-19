## ADDED Requirements

### Requirement: System persists a registry of jobs

The system SHALL persist a `Job` record per registered job with a unique `Key`, a cron expression, an `IsEnabled` flag, and `LastRunAt` / `NextRunAt` timestamps. The job registry is created from `IJob` implementations on startup.

#### Scenario: Register a job on startup

- **WHEN** an `IJob` is registered via `AddJob<T>` at startup
- **THEN** a `Job` row is created with its `Key`, `Cron`, `IsEnabled = true`, and a `NextRunAt` calculated from `Cron`

#### Scenario: Disabled job is not executed

- **WHEN** a job's `IsEnabled` is `false`
- **THEN** the scheduler does not start a run for that job

#### Scenario: Update cron

- **WHEN** an Admin updates a job's cron expression
- **THEN** the next run is recalculated from the new expression and persisted

### Requirement: Scheduler runs jobs on their cron schedule

The system SHALL evaluate each enabled job's cron expression and start a `JobRun` when the current tick matches the next due time.

#### Scenario: Tick triggers due job

- **WHEN** the scheduler tick reaches a job whose `NextRunAt` is in the past
- **THEN** a `JobRun` is created with `Status = Running`, the job's `ExecuteAsync` is invoked, and `NextRunAt` is recalculated for the next cycle

#### Scenario: Failed run is recorded

- **WHEN** an `IJob.ExecuteAsync` throws
- **THEN** the `JobRun` is recorded as `Status = Failed` with the exception message and the scheduler continues with the next job

### Requirement: Runs are idempotent per cycle

The system SHALL derive an `IdempotencyKey` from the job key and the cycle window so that an accidental double-tick does not execute the same cycle twice.

#### Scenario: Duplicate tick skipped

- **WHEN** the scheduler attempts to start a second run for the same job while a `JobRun` with the same `IdempotencyKey` exists in `Running` state
- **THEN** the second attempt is logged and skipped

#### Scenario: New cycle after success

- **WHEN** a run finishes successfully and the tick advances to the next cycle
- **THEN** the new cycle computes a fresh `IdempotencyKey` and is allowed to run

### Requirement: Job runs are serialised per job

The system SHALL acquire a per-job `LockToken` before starting a run so that overlapping cycles (e.g. slow job + scheduler restart) cannot run concurrently.

#### Scenario: Overlapping tick is blocked

- **WHEN** a previous run for the same job is still `Running`
- **THEN** the new tick does not start another run and records a `Skipped` JobRun row

#### Scenario: Crash releases lock

- **WHEN** the process hosting a `Running` JobRun terminates without writing a `FinishedAt`
- **THEN** on startup the scheduler marks the stale run as `Failed` and the next tick is allowed to proceed

### Requirement: Admin can inspect and operate jobs

The system SHALL allow an Admin to list jobs with last/next run, success rate, and to manually trigger a job, pause it, or resume it.

#### Scenario: View jobs

- **WHEN** an Admin opens the jobs page
- **THEN** all jobs are listed with `Key`, `IsEnabled`, `LastRunAt`, `NextRunAt`, last-run `Status`, and 7-day success rate

#### Scenario: View run history

- **WHEN** an Admin opens a job's detail page
- **THEN** the recent runs are listed with started/finished timestamps, status, error message, and idempotency key

#### Scenario: Manual run

- **WHEN** an Admin clicks "Run now" on a disabled job's detail page
- **THEN** a run is queued immediately and the page reflects the new `JobRun`

#### Scenario: Pause and resume

- **WHEN** an Admin pauses a job
- **THEN** the scheduler does not start runs for it until it is resumed

#### Scenario: Non-admin denied

- **WHEN** a non-admin user calls `/Admin/Jobs`
- **THEN** access is denied with a 403/redirect