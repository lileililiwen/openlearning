## Context

All concrete business jobs (close-unpaid-order, expire-enrollment, daily stats, settlement, etc.) need a substrate: durable storage, cron evaluation, idempotency, and a per-job lock to avoid overlapping runs. Today we have only ad-hoc `BackgroundService` loops (`LogRetentionWorker`, `MediaTranscoder`). This change ships the substrate; the `scheduled-business-jobs` change plugs in the actual jobs.

We will use Cronos (NuGet, MIT, zero-dep) for cron evaluation so we don't reinvent parsing/leap-year handling. The job store uses EF Core per the modular monolith pattern. Per-job locks are implemented via a `LockToken` column updated under a single SQL update (`UPDATE job SET lock_token=@t WHERE id=@id AND lock_token=@expected`) so they work correctly with a single Postgres instance; if we later move to multi-instance, swap to `pg_try_advisory_lock`.

## Goals / Non-Goals

**Goals:**
- Persistent `Job` + `JobRun` records, queryable by an Admin UI.
- Cron expression per job, recalculated `NextRunAt` on every successful tick.
- Exactly-once per cycle via an `IdempotencyKey` and a per-job `LockToken`.
- Manual `Run now` / `Pause` / `Resume` from the admin UI.
- Operation-log entries on each job-run outcome (reuses the existing `logging` capability).

**Non-Goals:**
- Multi-instance leader election (single-instance Postgres lock is sufficient today; revisit if we shard).
- A visual cron builder UI (out of scope; Admin enters cron text).
- Distributed tracing of job spans (out of scope; the operation log entry has job key + cycle).

## Decisions

- **Cronos for cron parsing**. Alternatives considered: write our own (rejected — bug surface), `NCrontab` (equivalent; Cronos has clearer docs).
- **`Job` table in a new `OpenLearning.Jobs` module**. Alternatives: store jobs in `system-config` JSON (rejected — needs atomic updates under concurrency, which JSON keys do not give us cleanly).
- **Idempotency key = `Key + floor(unixSeconds / cycleSeconds)`**, where `cycleSeconds` is the smallest window implied by the cron (e.g. `*/5 * * * *` → 300s). This makes accidental double-ticks safe.
- **Lock via `UPDATE … WHERE lock_token = @expected`** atomic compare-and-set; the losing tick writes a `Skipped` JobRun row.
- **Stale-run recovery on startup**: any `JobRun` left `Running` after a process crash is marked `Failed` so the next tick can proceed.
- **No retry policy at the scheduler level** — jobs decide their own retry. The scheduler records `Failed` once and waits for the next cycle.

## Risks / Trade-offs

- [Risk: clock skew on the host miscomputes `NextRunAt`] → Mitigation: use UTC; document that all cron expressions are UTC; tests cover DST boundaries.
- [Risk: a long-running job blocks its next cycle] → Mitigation: `Skipped` JobRun makes the overlap visible; per-job timeout (configurable, default 30min) prevents unbounded blocks.
- [Risk: Admin deletes a job row while a run is in flight] → Mitigation: deleting a job is not exposed; `IsEnabled = false` is the supported pause.
- [Risk: massive job-run history bloats the DB] → Mitigation: `JobRun` rows older than 30 days are pruned by `LogRetentionWorker` (coordinated via the logging module).

## Migration Plan

1. Add `OpenLearning.Jobs` module + `AddJobsModule()`.
2. Generate EF migration `AddJobs` for `Job` + `JobRun`.
3. Run on a fresh DB to verify schema; on an existing DB the migration adds the two empty tables.
4. Verify the Admin Jobs page renders empty; register one no-op `IJob` for a smoke test.
5. No business jobs register here; `scheduled-business-jobs` change adds them.

## Open Questions

- Should we expose a `POST /api/jobs/{key}/run` for ops scripts, or only the admin UI? Current decision: admin UI only for v1; revisit when ops scripts land.
- Should we record `JobRun.InputPayload` for jobs that take parameters? Out of scope here; jobs read from the DB at execution time.