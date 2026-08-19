## Context

The brief is explicit: sync imports/exports should cap at ~200 rows; everything larger must run in the background. With five different IO surfaces (questions, students, grades, outlines, coupons) about to ship, each would reinvent upload storage, status tracking, error file generation, and notification wiring. We centralise that into `OpenLearning.AsyncIO` so consumers focus on parsing and validation only.

`job-scheduler` already provides the substrate (cron, idempotency, lock, admin UI). `async-io-jobs` adds the IO-specific concerns: file lifecycle, owner-scoped visibility, retention, and notification hooks.

## Goals / Non-Goals

**Goals:**
- One submission API for all bulk IO.
- Owner-scoped job list and detail pages.
- Per-consumer validation rules (extension whitelist, max size).
- File retention via `scheduled-business-jobs`.
- Hooks for `import.completed` / `import.failed` / `export.ready` / `export.progress` notifications.

**Non-Goals:**
- A generic "upload anything" endpoint — each consumer wraps it with its own validation.
- Distributed execution across replicas (single-instance Postgres lock is sufficient; `job-scheduler` already covers this).

## Decisions

- **One `AsyncIOJob` table, `Kind` discriminator column**. Consumer modules query with `WHERE Kind = 'QuestionImport'` etc. Reason: avoids N tables of identical shape.
- **Validation pluggable per consumer**: each consumer implements `IIOFileValidator { Task ValidateAsync(IFormFile file); }` and is registered with `services.AddAsyncIOFileValidator<QuestionImportValidator>()`.
- **`IJob` instances are registered by the consumer**, not by `async-io-jobs`. `async-io-jobs` provides the dispatcher (`AsyncIOJobDispatcher`) that resolves the right `IJob` by `Kind` and invokes it.
- **Retention is a single scheduled job** registered by `scheduled-business-jobs`; per-consumer overrides via `system-config` (`asyncio.retention.days`, default 7).
- **Error file format is `.xlsx`** uniformly; consumers write the same columns (`RowIndex, Field, Message`).

## Risks / Trade-offs

- [Risk: a consumer forgets to call `AsyncIOService.SubmitAsync` and the upload is lost] → Mitigation: a code review checklist; integration tests assert that every async IO consumer extends a base class that calls `SubmitAsync`.
- [Risk: 5 consumers all register `IJob` keys with similar names and collide] → Mitigation: the `Kind` discriminator is the canonical key; consumer modules namespace their `IJob.Key` as `<kind>.process`.
- [Risk: a malicious file is processed before extension validation] → Mitigation: `ValidateAsync` runs before storage; rejections short-circuit.

## Migration Plan

1. Add `OpenLearning.AsyncIO` module + EF migration `AddAsyncIO`.
2. Each consumer change (`question-import-export`, `student-bulk-import`, `grade-export`, `course-outline-import-export`, `coupon-bulk-import`) replaces its bespoke upload logic with `AsyncIOService.SubmitAsync` + its own `IJob` and validator.
3. Verify retention on a synthetic file: upload, age the row in SQL, run the cleanup job, confirm the file is gone.

## Open Questions

- Should we expose a UI to retry a failed job? Currently the user re-uploads. Re-uploading is simpler and matches the brief's "正确数据入库；错误行收集到错误Excel" flow.
- Should `export.progress` notifications fire for jobs < 5 minutes? No — they only fire for longer jobs to avoid noise.