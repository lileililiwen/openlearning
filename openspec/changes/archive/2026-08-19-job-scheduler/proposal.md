## Why

Time-driven batch work (closing unpaid orders, expiring enrollments, generating daily statistics, periodic settlement) is the backbone of an LMS but currently there is no scheduled-job subsystem: the only background workers are one-off `BackgroundService` loops (`LogRetentionWorker`, `MediaTranscoder`) with no cron, no persistence, no concurrency control, and no UI. Every future batch need has to re-invent these guarantees. We need a single, durable, idempotent job scheduler so the 时间 dimension can be filled without ad-hoc plumbing.

## What Changes

- Introduce a generic `OpenLearning.Jobs` module providing a `JobStore` (persistent job + job-run records), a `JobScheduler` (cron expression evaluation, next-run calculation, tick loop), and an `IJob` contract with `IdempotencyKey` for exactly-once semantics per cycle.
- Provide a built-in `JobAdminService` for an Admin to view jobs, recent runs, success/failure counts, and to manually trigger / pause a job.
- Provide a `JobLock` mechanism that serialises runs per job across replicas and short-circuits overlapping ticks.
- Add no concrete business jobs in this change — only the substrate. The `scheduled-business-jobs` change will register the concrete jobs (unpaid-order close, refund timeout, enrollment expiry, reminders, statistics, settlement, coupon deactivation, log archive, certificate expiry).

## Capabilities

### New Capabilities

- `job-scheduler`: persistent job registry, cron-driven scheduling, idempotent execution, lock to prevent overlapping runs, admin visibility into job runs, manual trigger / pause / resume.

### Modified Capabilities

- `logging`: an executed job run records an operation log entry on success/failure so it appears in the existing admin logs pages without code duplication.

## Impact

- New `OpenLearning.Jobs` class library: `Job { Id, Key, Cron, IsEnabled, LastRunAt, NextRunAt }`, `JobRun { Id, JobId, StartedAt, FinishedAt?, Status (Running/Success/Failed), ErrorMessage?, IdempotencyKey, LockToken }`. New EF migration for these two tables.
- Services: `JobStore` (CRUD + queries), `JobScheduler : BackgroundService` (tick loop), `JobDispatcher` (resolves `IJob` by key and runs it under lock), `JobAdminService` (UI queries, manual trigger).
- `IJob` contract: `string Key { get; }`, `string Cron { get; }`, `Task ExecuteAsync(JobContext ctx, CancellationToken ct)`. Each business change registers its `IJob` implementations via `services.AddJob<MyJob>()`.
- Admin UI: `Pages/Admin/Jobs/Index.cshtml(.cs)` lists jobs with last/next run and success-rate; `/Admin/Jobs/{id}` shows recent runs with status/error; buttons for Run-now and Pause/Resume under the `AdminJobs` policy.
- One-line DI registration: `builder.Services.AddJobsModule();` in `Program.cs`.
- Architecture follows Agents.md §2.1 (modular monolith): no module references `OpenLearning.Data`; `OpenLearning.Data` references `OpenLearning.Jobs` to scan configs.