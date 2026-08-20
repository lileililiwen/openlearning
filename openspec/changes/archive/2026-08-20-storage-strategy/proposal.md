## Why

Storage is hardwired to the local disk: `AddStorageModule(storageRoot)` always
registers `LocalStorageProvider`, and the root comes from appsettings at
startup. Admins need to point uploads at mainstream OSS (Aliyun OSS, S3 /
MinIO) without code changes — the brief asks for a configurable upload
storage strategy. The blob seam already exists
(`IStorageProvider.Save/Open/Delete`), so this is a provider-registry +
configuration problem, not a rewrite.

## What Changes

- `IStorageProvider` gains three new implementations: `S3StorageProvider`
  (S3-compatible, including AWS S3 and MinIO — path-style forced for MinIO),
  `AliyunOssProvider` (OSS REST API, no third-party SDK), and a dedicated
  **MinIO** strategy (self-hosted built-in OSS via the S3 provider);
  `LocalStorageProvider` stays.
- A provider **factory** selects the active strategy **at startup** from
  admin-configured settings (system-config), so the change applies on the next
  restart — no hot data-plane swap.
- An admin **storage page** (`/Admin/Storage`) to choose the strategy, enter
  provider options (endpoint/bucket/access-key/secret/region/path-style), run a
  connectivity test, and save.
- Per-purpose upload **limits (max bytes + allowed extensions) become
  configurable** via system-config instead of the hardcoded `_limits`
  dictionary; `StorageService` reads them at upload time.
- The `/files/{key}` proxy and video renditions keep working for any backend:
  the proxy streams through the active provider, so existing URLs and rendition
  URLs (`files/...`) are unchanged.

## Capabilities

### New Capabilities

- `storage-strategy`: configurable storage backend selection + provider
  options + connectivity test.

### Modified Capabilities

- `storage`: `IStorageProvider` registry (Local/S3/Aliyun), startup factory,
  configurable per-purpose limits, renditions written through the active
  provider.
- `system-config`: whitelist gains the storage strategy/provider option keys
  and the per-purpose limit keys.

## Impact

- Storage module changes: `S3StorageProvider` (AWSSDK.S3),
  `AliyunOssProvider` (Aliyun.OSS.SDK), `StorageProviderFactory`, limits read
  from `SystemConfigService`.
- New `Pages/Admin/Storage.cshtml(.cs)` (RequireAdmin) + a menu entry; the page
  persists provider options as system-config settings and shows a
  "restart required" banner after saving.
- No schema migration — settings live in the existing `Settings` table.
- NuGet additions: `AWSSDK.S3`, `Aliyun.OSS.SDK` (pinned versions).
