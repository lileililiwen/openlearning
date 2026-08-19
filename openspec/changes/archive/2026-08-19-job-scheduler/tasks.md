## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Jobs` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth` only (never `OpenLearning.Data`)
- [x] 1.2 Add `Job { Id, Key (unique), Cron, IsEnabled, LastRunAt?, NextRunAt, LockToken? }` and `JobRun { Id, JobId, StartedAt, FinishedAt?, Status (enum: Running/Success/Failed/Skipped), ErrorMessage?, IdempotencyKey, LockToken }` entities + `IEntityTypeConfiguration<T>` each
- [x] 1.3 Implement `IJob` contract: `string Key`, `string Cron`, `TimeSpan Timeout` (default 30min), `Task ExecuteAsync(JobContext, CancellationToken)`
- [x] 1.4 Implement `JobStore` (CRUD + queries: list jobs, get by key, list runs)
- [x] 1.5 Implement `JobScheduler : BackgroundService` (tick every 30s) with cron evaluation via Cronos
- [x] 1.6 Implement `JobDispatcher`: acquire `LockToken` via compare-and-set; insert `JobRun` with `IdempotencyKey = Key + cycle`; invoke `IJob.ExecuteAsync`; update `JobRun.Status` and `Job.LastRunAt`/`Job.NextRunAt`
- [x] 1.7 On startup, scan `IJob` implementations from DI, upsert `Job` rows (create-if-missing, update `Cron`/`IsEnabled` if changed)
- [x] 1.8 Stale-run recovery: on startup, mark any `JobRun` left `Running` as `Failed`
- [x] 1.9 Wire operation-log entries via `LogService.LogAsync` on each `JobRun` outcome (success/failed/skipped) — reuse `OpenLearning.Logging.Services.LogService`
- [x] 1.10 Register `AddJobsModule` in `Program.cs` (one line)
- [x] 1.11 Add `OpenLearning.Data` reference to `OpenLearning.Jobs` ONLY for the assembly-config scan line (per §2.1 gotcha — handled via `JobsDataRegistration`)

## 2. EF Migration & Build

- [x] 2.1 Add EF migration `AddJobs` via `dotnet ef migrations add AddJobs --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.2 Confirm `dotnet build OpenLearning.sln` is 0 warnings / 0 errors
- [x] 2.4 Apply migration on dev DB and confirm the two tables exist

## 3. Admin UI

- [x] 3.1 Create `Pages/Admin/Jobs/Index.cshtml(.cs)` listing jobs with `Key`, `IsEnabled`, `LastRunAt`, `NextRunAt`, last `Status`, 7-day success rate; policy `AdminJobs`
- [x] 3.2 Create `Pages/Admin/Jobs/Detail.cshtml(.cs)` with recent `JobRun` list and a "Run now" button
- [x] 3.3 Implement Pause / Resume POST handlers that flip `IsEnabled`
- [x] 3.4 Verify a non-admin user is denied with 403/redirect

## 4. Smoke Tests

- [x] 4.1 Register a no-op `IJob` with cron `*/1 * * * * *` (every second) and verify it runs exactly once per cycle (IdempotencyKey skips double-ticks)
- [x] 4.2 Manually trigger Run-now from the admin UI and confirm a `JobRun` row appears with `Success`
- [x] 4.3 Force a failure (throw inside `ExecuteAsync`) and confirm the run is `Failed` with the exception message, and the operation log has an entry
- [x] 4.4 Pause a job from the admin UI and verify no further ticks happen until Resumed
- [x] 4.5 Confirm DB rows: `Job` and `JobRun` tables populated, indexes on `Job.Key` and `JobRun.JobId+StartedAt` exist
- [x] 4.6 Confirm `dotnet build OpenLearning.sln` is 0 warnings / 0 errors after UI changes

## 5. Documentation

- [x] 5.1 Update `Agents.md` §2 module list to include `OpenLearning.Jobs`
- [x] 5.2 Update `README.md` operations section to mention job-scheduler (where to view runs)