## Context

`job-scheduler` (this repo's first new change) provides the substrate: persistent jobs, cron ticks, idempotency, locks, admin UI. This change plugs the concrete business jobs the brief enumerates. The jobs live in their owning modules (`OpenLearning.Ecommerce.Jobs.OrderExpireUnpaidJob`, etc.) per the modular monolith pattern; the change is mostly additive service methods and a registration block in `Program.cs`.

We split jobs per owning module rather than one big `OpenLearning.Jobs` module because each module owns its own data and the change should not create cross-module dependencies (per Agents.md §2). Idempotency is per-row state (e.g. an `Order.ProcessedAt` flag added to the close-unpaid job's flow) layered on top of the `IdempotencyKey` provided by `job-scheduler`.

## Goals / Non-Goals

**Goals:**
- Ship every batch job the brief calls out, each with a documented cron and idempotency contract.
- Expose each job on the admin Jobs page (job-scheduler) so operators can run-now / pause / resume.
- Add the per-module service methods the jobs need.

**Non-Goals:**
- A scheduler UI (job-scheduler already provides it).
- Per-tenant cron overrides.
- Distributed execution across replicas (single-instance Postgres lock is sufficient).

## Decisions

- **Each module owns its `IJob` implementations** under `OpenLearning.<Module>/Jobs/`. Reason: keeps data access local; matches the §2 pattern.
- **`Program.cs` registers each job in one block** — `services.AddJob<OrderExpireUnpaidJob>(); services.AddJob<RefundTimeoutCloseJob>(); …` — visible in code review, easy to pause individually by commenting out.
- **Idempotency is two-layered**:
  1. `IdempotencyKey` from `job-scheduler` prevents accidental double-ticks.
  2. Per-row state (e.g. `Order.CancelledAt` for close-unpaid, `RefundRequest.ResolvedAt` for refund-timeout) prevents repeated processing after a crash.
- **Cron expressions in `Program.cs` are documented in a single table** near the registration block so operators can tune them without searching modules.
- **Failed jobs do not block other jobs** — `JobScheduler` (from job-scheduler) continues to the next job even if one throws.

## Risks / Trade-offs

- [Risk: jobs fire during high traffic and contend on hot tables] → Mitigation: each job has a configurable batch size; the `order.expire-unpaid` job processes 200 orders per tick instead of all-in-one.
- [Risk: a job runs before its data dependency (e.g. settlement before `study.daily-aggregate`)] → Mitigation: cron expressions are sequenced (analytics after study, settlement after analytics). Documented.
- [Risk: too many notifications fired at once from `enrollment.expiry.notify-soon`] → Mitigation: per-user rate limiting via `notifications` (existing throttle on channel dispatch, documented in `messaging-channels`).
- [Risk: jobs accumulate lag when the process is down for hours] → Mitigation: `job-scheduler` tick catches up by running jobs whose `NextRunAt` is in the past, capped at one catch-up run per cycle to avoid thundering herd.
- [Risk: log archive deletes too aggressively] → Mitigation: archive window is configurable via `logging.retention.days`; default is 90 days; only logs older than that are deleted.

## Migration Plan

1. Land `job-scheduler` first (one change) so cron registration has somewhere to plug in.
2. Land per-module service methods needed by the jobs (`OrderService.ExpireUnpaidAsync`, etc.).
3. Land `IJob` classes + `Program.cs` registrations for each job.
4. Verify each job on the admin Jobs page (Run-now, inspect JobRun).
5. Add a nightly smoke test that runs every job once a day and verifies no failures.

## Open Questions

- Should we expose job-result metrics to `platform-analytics`? Useful but out of scope here; revisit after settlement reporting is in.
- Should `coupon.expire-disabled` also delete the coupon row? No — keep history; only `IsActive` flips.