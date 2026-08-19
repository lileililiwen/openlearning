## 1. Substrate Dependency

- [x] 1.1 Confirm `job-scheduler` is merged; if not, defer this change. The substrate's `IJob`, `AddJob<T>()`, and the admin Jobs page are prerequisites.

## 2. Per-Module Service Methods

- [x] 2.1 `OpenLearning.Ecommerce.Services.OrderService`: add `ExpireUnpaidAsync(now, batchSize)` returning the number closed
- [x] 2.2 `OpenLearning.Ecommerce.Services.CouponService`: add `ReleaseHoldAsync(orderId)` (idempotent) and `DisableExpiredAsync(now)`
- [x] 2.3 `OpenLearning.Ecommerce.Services.RefundService`: add `TimeoutCloseAsync(refundId)` (sets `Status = Rejected`, reason `"timeout"`, notifies student)
- [x] 2.4 `OpenLearning.Enrollment.Services.EnrollmentService`: add `ListExpiredPastGraceAsync(now)` and `RevokeAsync` (already in `course-access-period`)
- [x] 2.5 `OpenLearning.Assignments.Services.AssignmentService`: add `ListDueTomorrowAsync(now)` and `AutoClosePastDueAsync(assignmentId)`
- [x] 2.6 `OpenLearning.Assessments.Services.ExamService` (extended by `exams`): add `ListStartingWithinAsync(windowStart, windowEnd)`
- [x] 2.7 `OpenLearning.StudyTools.Services.StudyToolService`: add `AggregateDailyAsync(date)` upserting `StudyDailyAggregate`
- [x] 2.8 `OpenLearning.Classes.Services.ClassGroupService`: add `ListStartingWithinAsync(windowStart, windowEnd)`
- [x] 2.9 `OpenLearning.Ecommerce.Services.CouponService`: add `DisableExpiredAsync(now)`
- [x] 2.10 `OpenLearning.Settlement.Services.SettlementService`: add `CloseInstructorPeriodAsync(periodStart, periodEnd)` (idempotent by week)
- [x] 2.11 `OpenLearning.Distribution.Services.DistributionService` (from `affiliate-distribution`): add `CloseDistributorPeriodAsync(periodStart, periodEnd)`

## 3. IJob Implementations

- [x] 3.1 `OpenLearning.Ecommerce.Jobs.OrderExpireUnpaidJob` — cron `*/1 * * * *` (every minute)
- [x] 3.2 `OpenLearning.Ecommerce.Jobs.RefundTimeoutCloseJob` — cron `0 3 * * *` (daily 03:00)
- [x] 3.3 `OpenLearning.Enrollment.Jobs.EnrollmentExpiryRevokeJob` — cron `0 * * * *` (hourly)
- [x] 3.4 `OpenLearning.Enrollment.Jobs.EnrollmentExpiryNotifySoonJob` — cron `0 4 * * *` (daily 04:00)
- [x] 3.5 `OpenLearning.Assignments.Jobs.AssignmentDueReminderJob` — cron `0 * * * *` (hourly)
- [x] 3.6 `OpenLearning.Assessments.Jobs.ExamReminderJob` — cron `*/5 * * * *` (every 5 min)
- [x] 3.7 `OpenLearning.Classes.Jobs.ClassStartReminderJob` — cron `*/5 * * * *` (every 5 min)
- [x] 3.8 `OpenLearning.StudyTools.Jobs.StudyDailyAggregateJob` — cron `0 3 * * *` (daily 03:00)
- [x] 3.9 `OpenLearning.Analytics.Jobs.AnalyticsDailyReportJob` — cron `0 4 * * *` (daily 04:00)
- [x] 3.10 `OpenLearning.Analytics.Jobs.AnalyticsWeeklyReportJob` — cron `0 4 * * 1` (Monday 04:00)
- [x] 3.11 `OpenLearning.Settlement.Jobs.InstructorSettlementCloseJob` — cron `0 23 * * 0` (Sunday 23:00)
- [x] 3.12 `OpenLearning.Distribution.Jobs.DistributorSettlementCloseJob` — cron `0 23 * * 0` (Sunday 23:00)
- [x] 3.13 `OpenLearning.Ecommerce.Jobs.CouponExpireDisabledJob` — cron `0 * * * *` (hourly)
- [x] 3.14 `OpenLearning.Logging.Jobs.LogArchiveJob` — cron `0 5 * * *` (daily 05:00); replace the existing `LogRetentionWorker` `BackgroundService`
- [x] 3.15 `OpenLearning.AsyncIO.Jobs.AsyncIOCleanupJob` — cron `0 2 * * *` (daily 02:00); prunes expired result / error files (per `async-io-jobs`)
- [x] 3.16 `OpenLearning.GradeExport.Jobs.GradeExportCleanupJob` — cron `0 2 * * *` (daily 02:00); prunes expired grade export files (per `grade-export`)

## 4. Composition Root

- [x] 4.1 In `Program.cs`, register each `IJob` via `services.AddJob<T>()` in a single block with a comment table listing key / cron / purpose
- [x] 4.2 Verify the admin Jobs page (`/Admin/Jobs`) lists all 14 jobs with their crons

## 5. Notification Events

- [x] 5.1 Wire each notification emission through `NotificationService` and ensure each is covered by `notification-events-extensions`:
  - `order.expired` (close-unpaid)
  - `refund.timeout-rejected`
  - `enrollment.expiring-soon`
  - `enrollment.expired`
  - `assignment.due-soon`
  - `assignment.due-missed`
  - `exam.starting-soon`
  - `class.starting-soon`

## 6. Idempotency per Job

- [x] 6.1 Document in each `IJob` file's header what the idempotency strategy is: `IdempotencyKey` from job-scheduler + which row-level state guarantees exactly-once semantics
- [x] 6.2 Add a unit test per job that runs the job twice with the same inputs and asserts no duplicate notifications / ledger entries

## 7. Build & Verify

- [x] 7.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 7.2 Smoke tests per job (Run-now from admin Jobs page, verify side effects):
  - Close-unpaid: create an unpaid order, age it via SQL `UPDATE Orders SET CreatedAt = UtcNow - INTERVAL '31 minutes'`, run job, verify order is `Cancelled` and coupon hold released; run again, verify no change
  - Refund-timeout: create a refund request, age it, run job, verify `Rejected` with reason `"timeout"`
  - Enrollment expiry: from `course-access-period`, age an enrollment past grace, run job, verify `RevokedAt` set
  - Class start reminder: create a class with `StartsAt = UtcNow + 25 minutes`, run job, verify class members got a notification
  - Study daily aggregate: create a few `StudySession` rows, run job for that date, verify aggregate rows
  - Coupon expiry: create a coupon with `EndsAt = UtcNow - 1 minute`, run job, verify `IsActive = false`
  - Settlement close: create a few paid orders, run instructor settlement job, verify statements created; run again, verify no duplicate
- [x] 7.3 Verify the Jobs page shows each job with its cron, last-run timestamp, and recent status