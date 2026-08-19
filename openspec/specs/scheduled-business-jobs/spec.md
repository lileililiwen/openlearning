# scheduled-business-jobs Specification

## Purpose
TBD - created by archiving change scheduled-business-jobs. Update Purpose after archive.
## Requirements
### Requirement: Unpaid orders are auto-closed

The system SHALL run an `order.expire-unpaid` job every minute that closes orders unpaid after 30 minutes and releases any reserved coupon / balance hold.

#### Scenario: Order closed after 30 minutes

- **WHEN** an order is unpaid and `CreatedAt` is older than 30 minutes
- **THEN** the job marks the order `Cancelled` and releases the coupon hold

#### Scenario: Idempotent run

- **WHEN** the job runs twice in the same minute
- **THEN** the second run is a no-op (idempotency key covers it; no second release)

### Requirement: Refund requests auto-close on timeout

The system SHALL run a `refund.timeout-close` job daily that closes refund requests not reviewed within 7 days and notifies the student that the refund was not approved.

#### Scenario: Auto-close on day 7

- **WHEN** a refund request is `Pending` and `RequestedAt` is older than 7 days
- **THEN** the job marks it `Rejected` with reason "timeout" and notifies the student

#### Scenario: Already-reviewed refunds skipped

- **WHEN** a refund request is already `Approved` or `Rejected`
- **THEN** the job does not touch it

### Requirement: Enrollment expiry revocation

The system SHALL run an `enrollment.expiry.revoke` job hourly that revokes enrollments past `AccessExpiresAt + graceDays` (per `course-access-period`).

#### Scenario: Revoke past grace

- **WHEN** an enrollment is past `AccessExpiresAt + graceDays`
- **THEN** the job sets `RevokedAt = UtcNow`, `RevokedReason = "expired"`, and notifies the learner

#### Scenario: Already-revoked skipped

- **WHEN** an enrollment is already `Revoked`
- **THEN** the job does not touch it

### Requirement: Expiry-soon notification

The system SHALL run an `enrollment.expiry.notify-soon` job daily that notifies learners whose enrollments expire within 7 days.

#### Scenario: T-7 day notification

- **WHEN** an enrollment expires in 7 days
- **THEN** the learner receives a `enrollment.expiring-soon` notification

#### Scenario: Do not re-notify

- **WHEN** the job already notified within the last 24h (stored as a notification row)
- **THEN** it does not notify again

### Requirement: Assignment due reminders

The system SHALL run an `assignment.due-reminder` job hourly that reminds students of assignments due within 24 hours and auto-closes submissions for past-due assignments.

#### Scenario: T-24h reminder

- **WHEN** an assignment is due within 24h
- **THEN** each enrolled student who has not yet submitted receives a reminder notification

#### Scenario: Auto-close past due

- **WHEN** an assignment is past its due date
- **THEN** the job closes the submission endpoint (`ClosesAt = now`) and notifies students who did not submit

### Requirement: Exam reminder

The system SHALL run an `exam.reminder` job every 5 minutes that reminds students of an exam whose `StartsAt` is within 30 minutes.

#### Scenario: T-30min reminder

- **WHEN** an exam's `StartsAt` is in 30 minutes
- **THEN** every enrolled student who has not yet attempted receives a reminder

#### Scenario: Once per exam

- **WHEN** the job already reminded for the exam within the last 30 min
- **THEN** it does not notify again

### Requirement: Class start reminder

The system SHALL run a `class.start-reminder` job every 5 minutes that reminds members of a `ClassGroup` whose `StartsAt` is within 30 minutes (per `class-groups`).

#### Scenario: T-30min class reminder

- **WHEN** a class's `StartsAt` is in 30 minutes
- **THEN** every enrolled student receives a class-scoped announcement

### Requirement: Daily study aggregation

The system SHALL run a `study.daily-aggregate` job daily at 03:00 (server time) that aggregates `StudySession` rows into a per-day, per-course, per-student summary for analytics.

#### Scenario: Daily aggregate produced

- **WHEN** the job runs
- **THEN** `StudyDailyAggregate { Date, UserId, CourseId, TotalSeconds, LessonsCompleted }` rows are upserted for the previous day

#### Scenario: Idempotent on same day

- **WHEN** the job runs twice for the same day
- **THEN** the existing rows are overwritten (idempotent)

### Requirement: Daily platform report

The system SHALL run an `analytics.daily-report` job daily at 04:00 that generates a `PlatformDailyReport` (orders, signups, completion rate) for the previous day.

#### Scenario: Report written

- **WHEN** the job runs
- **THEN** a `PlatformDailyReport { Date, Orders, Signups, CompletionRate, TotalRevenue }` row is written

#### Scenario: Admin can view reports

- **WHEN** an Admin opens the analytics report list
- **THEN** the daily reports are listed newest first

### Requirement: Weekly class report

The system SHALL run an `analytics.weekly-report` job weekly on Monday at 04:00 that aggregates per-class weak-knowledge points using assignment scores and exam outcomes.

#### Scenario: Weekly class report

- **WHEN** the job runs for week N
- **THEN** a `ClassWeeklyReport { ClassGroupId, WeekStart, WeakTopicsJson }` row is written per class group

### Requirement: Periodic instructor settlement

The system SHALL run a `settlement.instructor-period-close` job weekly (default Sunday 23:00) that freezes the week's instructor earnings into a `SettlementStatement` (per `instructor-revenue` patterns).

#### Scenario: Freeze the week

- **WHEN** the job runs for week N
- **THEN** every instructor with non-zero earnings has a `SettlementStatement` created for week N

#### Scenario: Idempotent close

- **WHEN** the job runs twice for the same week
- **THEN** the second run is a no-op (idempotency key covers it)

### Requirement: Periodic distributor settlement

The system SHALL run a `settlement.distributor-period-close` job weekly (per `affiliate-distribution`) that freezes the week's distributor commissions.

#### Scenario: Freeze distributor period

- **WHEN** the job runs for week N
- **THEN** every distributor with non-zero earnings has a `SettlementStatement` row created

### Requirement: Coupon expiry

The system SHALL run a `coupon.expire-disabled` job hourly that disables coupons past their `EndsAt`.

#### Scenario: Disable expired coupon

- **WHEN** a coupon's `EndsAt < UtcNow`
- **THEN** the coupon is marked `IsActive = false`

### Requirement: Log archive / prune

The system SHALL run a `logs.archive` job daily that prunes log rows older than the configured retention period (existing `LogRetentionWorker` behaviour, migrated to `IJob`).

#### Scenario: Prune old logs

- **WHEN** the job runs
- **THEN** `OperationLog` and `ErrorLog` rows older than the retention period are deleted

#### Scenario: Retention configurable

- **WHEN** an Admin updates `logging.retention.days`
- **THEN** the next job run uses the new retention

### Requirement: Async IO file retention

The system SHALL run an `async-io.cleanup` job daily that prunes result and error files for finished async IO jobs older than the per-consumer retention period (default 7 days, per `async-io-jobs`).

#### Scenario: Prune expired files

- **WHEN** the job runs
- **THEN** `AsyncIOJob.ResultFileKey` and `AsyncIOJob.ErrorFileKey` are cleared for jobs older than the retention period and the underlying files are deleted from storage

#### Scenario: Retention configurable

- **WHEN** an Admin updates `asyncio.retention.days`
- **THEN** the next job run uses the new retention

### Requirement: Grade export file retention

The system SHALL run a `grade.export.cleanup` job daily that deletes exported grade files older than `grade.export.retentionDays` (default 7 days, per `grade-export`).

#### Scenario: Prune expired grade exports

- **WHEN** the job runs
- **THEN** `GradeExportJob.FileKey` is cleared for jobs older than the retention period and the underlying files are deleted

### Requirement: Jobs are idempotent per cycle

Every job in this change SHALL be idempotent: re-running within the same cycle is a no-op. The `IdempotencyKey` mechanism (per `job-scheduler`) covers accidental double-ticks; jobs additionally track per-row state to handle restart recovery.

#### Scenario: Restart after partial work

- **WHEN** a job crashed mid-run and restarts
- **THEN** partially-processed rows are detected (via row state, e.g. `ProcessedAt` column) and re-processing does not duplicate notifications or ledger entries

