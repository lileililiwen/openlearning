# Proposal: Offline Sync and Resilience

## Why

The mobile API specification does not establish offline behavior, queued progress, conflict handling, or clear sync status. Kolibri demonstrates an offline-first learning model for unreliable networks. OpenLearning needs a smaller, standards-based resilience slice for lessons and progress, and the current server contract only supports single-item sync — clients with a backlog must round-trip once per mutation, which is brittle on flaky networks and has no path to report mixed-success batches. Without per-item outcomes (`applied` / `duplicate` / `rejected` / `conflict`) the UI cannot show the learner what is still pending, and without `Lesson.ContentRevision` the server cannot tell stale from fresh when a client resyncs a queued event after an instructor republishes.

## What Changes

- Add `Lesson.ContentRevision` (int, concurrency token, default 1) and bump it on every lesson edit and on every course (re)publish so cached clients can detect when their copy is stale.
- Extend the progress sync request with an optional `ContentRevision` field; a stale value (`eventRevision < currentRevision`) is retained as a `conflict` with `{lessonId, currentRevision, eventRevision}` in `CanonicalState` instead of silently applying progress.
- Add `MobileSyncService.SyncBatchAsync` and `POST /api/mobile/v1/sync/batch` for batched sync with per-item results in input order. Each item is processed independently in its own `try`/`catch`; one bad item never aborts the rest of the batch, and an unknown `Type` returns `rejected` with the parse error captured in `CanonicalState`.
- Add unit tests covering retries, duplicates, stale content revision, current revision, legacy (no-revision) clients, batch happy path, and batch partial failure with valid items retained.
- Refactor `MobileSyncService` so the existing single-item endpoints share one `RecordAsync` write path with the batch endpoint, removing duplicated `SyncOperation.Add` + `SaveChanges` blocks.

## Non-goals

- Full offline authoring.
- Arbitrary offline access to paid/private content without entitlement checks.
- Peer-to-peer content distribution.
- Mobile client UI and storage. The client-side behavior (bounded cache, local queue, retry policy, network transitions, sync status UI) lives in the mobile client repo and is tracked outside this OpenSpec change.

## Impact

Adds a new `Lessons.ContentRevision` column with a one-time backfill to default 1, a new `BatchSyncItem` / `BatchSyncRequest` / `BatchSyncResponse` contract, a new `/api/mobile/v1/sync/batch` endpoint, and the corresponding tests. The existing single-item `/api/mobile/v1/sync/progress` and `/api/mobile/v1/sync/notes` endpoints continue to work; they now also accept the optional `ContentRevision` field on progress events.
