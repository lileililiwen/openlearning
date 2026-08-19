## Why

The brief lists a long catalog of time-driven batch work that today has no scheduler (no Quartz/Hangfire, no cron, only ad-hoc `BackgroundService` loops). The `job-scheduler` change ships the substrate; this change ships the concrete `IJob` implementations and the cron registrations that the brief enumerates. They are deliberately split so the scheduler stays generic and the business jobs stay decoupled.

## What Changes

Wire eleven batch jobs to the `job-scheduler` substrate. Each is its own `IJob` class registered with a cron expression and a description, visible on the admin Jobs page (job-scheduler).

1. `order.expire-unpaid` — close orders unpaid after 30 min and release any reserved coupon / balance hold.
2. `refund.timeout-close` — close refund requests not reviewed within 7 days (auto-rejected).
3. `enrollment.expiry.revoke` — revoke enrollments past their grace period (from `course-access-period`).
4. `enrollment.expiry.notify-soon` — T-7 days notification for expiring enrollments.
5. `assignment.due-reminder` — T-24h reminder for assignments due tomorrow; auto-close submission for past-due assignments.
6. `exam.reminder` — T-30min reminder for an exam scheduled to start within 30 min (uses `exams`/`live-streaming`).
7. `class.start-reminder` — T-30min reminder for a `ClassGroup` whose `StartsAt` is in 30 min (uses `class-groups`).
8. `study.daily-aggregate` — daily aggregation of `study-duration` sessions into a per-day, per-course, per-student summary.
9. `analytics.daily-report` — daily platform report (orders, signups, completion rate).
10. `analytics.weekly-report` — weekly class report (per-class薄弱知识点 summary) using class aggregates.
11. `settlement.instructor-period-close` — weekly/monthly freeze of instructor earnings → `SettlementStatement` (uses `instructor-revenue` patterns).
12. `settlement.distributor-period-close` — distributor settlement (uses `affiliate-distribution`).
13. `coupon.expire-disabled` — disable coupons past `EndsAt`.
14. `logs.archive` — older log rows archived / pruned (existing `LogRetentionWorker` migrated to `IJob`).

## Capabilities

### New Capabilities

- `scheduled-business-jobs`: the catalog of `IJob` implementations, their cron registrations, and the idempotency contract each must satisfy.

### Modified Capabilities

- `ecommerce`: `OrderService` exposes `ExpireUnpaidAsync(orderId)` and `ReleaseCouponHoldAsync(orderId)` for the close-unpaid job.
- `commerce-extras`: `RefundService` exposes `TimeoutCloseAsync(refundId)` for the refund-timeout job.
- `assignments`: `AssignmentService` exposes `ListDueTomorrowAsync()`, `AutoClosePastDueAsync(assignmentId)`.
- `exams`: `ExamService` exposes `ListStartingWithinAsync(windowMinutes)`.
- `study-duration`: `StudySessionService` exposes `AggregateDailyAsync(date)`.
- `platform-analytics`: `ReportService` exposes `GenerateDailyAsync(date)` and `GenerateWeeklyAsync(weekStart)`.
- `logging`: `LogRetentionWorker` becomes an `IJob` named `logs.archive` (replaces the inline loop).
- `coupons` (commerce-extras): `CouponService` exposes `DisableExpiredAsync(now)`.
- `instructor-revenue` (archived spec): `SettlementService` exposes `ClosePeriodAsync(periodStart, periodEnd)` (already similar; this change wires the cron).
- `affiliate-distribution` (proposed): `DistributionService` exposes `ClosePeriodAsync` (the job hook already ships there).

## Impact

- All jobs live in a new `src/OpenLearning.Scheduling` (or co-located with the owning module's `Jobs/` folder). Convention: each module owns its own `IJob` classes under `OpenLearning.<Module>.Jobs/`.
- The composition root `Program.cs` registers each `IJob` via `services.AddJob<OrderExpireUnpaidJob>()` (provided by `job-scheduler`).
- One EF migration `AddJobSchedules` adds a `JobSchedule` table for cron override persistence — but only if `job-scheduler` didn't add one. Confirm during integration.
- No new tables for the work itself; the jobs read from existing entities.
- Each job is idempotent: documented in `tasks.md` per job with the test that proves it.
- `IJob.LastRunAt` / `NextRunAt` are visible on the admin Jobs page (job-scheduler).