## Context

The blob abstraction is already clean: keys are server-generated
`{purpose}/{guid}{ext}` paths, `StorageService` is the only upload caller, and
`MediaTranscoder` writes renditions through the same `IStorageProvider`. Today
the provider is fixed at startup from `Storage:Root`. The cleanest way to let
an admin choose a strategy is a **startup-resolved factory**: `AddStorageModule`
registers `IStorageProvider` as a singleton whose implementation is chosen by
reading the persisted `Storage.Provider` setting the first time it is resolved
(after the app has migrated and seeded). Because the factory reads the DB, the
"apply on next startup" contract is natural: the admin saves settings, the app
restarts, and the next process picks the new provider.

Provider options (endpoints, buckets, keys, region, path-style) are stored as
system-config settings the admin page edits. Access keys are secrets — they are
stored server-side only, never rendered in full after save, and never logged.
The connectivity test writes/reads/deletes a probe object against the
configured provider.

Serving transparency: the existing `GET /files/{**key}` proxy calls
`IStorageProvider.OpenAsync`, so once the active provider is an OSS backend the
proxy streams from OSS and existing URLs (including `MediaAsset.Low/Mid/HighUrl`
which stay relative `files/...` keys) keep working unchanged. Renditions are
written through the same active provider.

Per-purpose limits move from the static `StorageService._limits` dictionary to
system-config keys (`Storage.Limits.<Purpose>.MaxBytes` and
`Storage.Limits.<Purpose>.Extensions`) with the current values as fallbacks.

## Goals / Non-Goals

**Goals:**
- Strategy selection: Local / S3-compatible (incl. MinIO) / Aliyun OSS.
- Admin configuration UI with a connectivity test.
- Change applies on next startup.
- Configurable per-purpose limits.
- No changes to existing URLs, the upload callers, or the resource center.

**Non-Goals:**
- Hot-swapping the active provider while serving traffic.
- Automatic migration of already-stored blobs between backends (admin may
  move them out-of-band; the proxy serves the active backend only).
- CDN URL rewriting / signed-URL generation (the proxy keeps a single origin).
- Provider auto-discovery or billing/metrics.

## Decisions

- **Factory resolves lazily on first `IStorageProvider` use** (singleton), so
  the DB is migrated before the strategy is read. Strategy + options are read
  from system-config; `Storage.Provider` defaults to `Local`.
- **Keys keep the `{purpose}/{guid}{ext}` shape on every backend** (S3 bucket
  key, OSS object key) so `StoredFile.Key` and the proxy are backend-agnostic.
- **Path-style S3 addressing option** for MinIO; region defaults to
  `us-east-1` but is configurable.
- **Secrets**: saved access keys are masked in the UI after save
  (`••••`), stored plaintext server-side in `Settings` (documented risk),
  excluded from any log output; a "clear key" action is provided.
- **Connectivity test** writes `storage-probe/{guid}` and deletes it; failure
  surfaces the provider error without logging secrets.

## Risks / Trade-offs

- [Risk: access keys stored in the DB] → Mitigation: admin-only page, masked
  display, never logged; the platform already stores dev secrets in
  appsettings; documented as accepted for an internal admin surface.
- [Risk: switching backends orphans existing blobs] → Mitigation: the
  strategy page shows the active backend and a warning that files uploaded
  under another backend are not migrated automatically.
- [Risk: S3/Aliyun SDK analyzers and package drift] → Mitigation: pin exact
  versions and confirm the build gate (0 warnings) still passes.
- [Risk: the lazy singleton resolves before the DB is ready] → Mitigation:
  the provider is only constructed when the first storage operation runs,
  which happens after startup migration.

## Migration Plan

1. Add `StorageProviderFactory` + `S3StorageProvider` + `AliyunOssProvider` to
   the Storage module; keep `LocalStorageProvider`.
2. Switch `AddStorageModule` to register the lazy factory; keep
   `Storage:Root` as the Local root (now read by the factory from config).
3. Add the configurable-limits path in `StorageService` (system-config
   fallbacks = current `_limits`).
4. Add `/Admin/Storage` + settings whitelist entries + menu entry.
5. Smoke-test Local unchanged, then MinIO (S3) and Aliyun OSS if credentials
   are available; verify `/files`, renditions, and uploads end-to-end.

## Open Questions

- Should provider credentials live in appsettings secrets instead of the DB?
  Decision: DB via admin UI (the brief says "the admin can configure"); an
  appsettings override (`Storage.Provider`) wins if present, so operators can
  still pin strategy in config.
- Should the OSS providers support presigned GET URLs for large videos?
  Out of scope; the proxy streams through the backend.
