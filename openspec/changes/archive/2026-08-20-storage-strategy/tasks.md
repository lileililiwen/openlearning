## 1. Provider Registry

- [x] 1.1 Add `S3StorageProvider : IStorageProvider` in the Storage module (AWSSDK.S3 pinned; custom endpoint + path-style + region + bucket; Save/Open/Delete by key)
- [x] 1.1b Add the explicit `MinIO` strategy (maps to `S3StorageProvider` with path-style forced true, default endpoint `http://localhost:9000`)
- [x] 1.2 Add `AliyunOssProvider : IStorageProvider` (OSS REST API via HttpClient + HMAC-SHA1 signing, no third-party SDK; CA5350/S4790 suppressed for the protocol-mandated HMAC-SHA1)
- [x] 1.3 Add `StorageProviderFactory` — reads `Storage.Provider` (+ provider options) from `SystemConfigService`, returns `LocalStorageProvider` (root from `Storage:Root`/config), `S3StorageProvider` (or the `MinIO` variant), or `AliyunOssProvider`; appsettings `Storage:Provider` overrides DB setting
- [x] 1.4 Rewire `AddStorageModule` to register `IStorageProvider` via the lazy factory (singleton, resolved after migration); keep `LocalStorageProvider` root path behavior
- [x] 1.5 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 2. Configurable Limits

- [x] 2.1 `StorageService` reads per-purpose `MaxBytes`/`Extensions` from `SystemConfigService` (`Storage.Limits.<Purpose>.MaxBytes` / `.Extensions`) with current defaults as fallback
- [x] 2.2 Limit keys are edited on the `/Admin/Storage` page (validated: positive int MB, extension list regex); no Admin/System whitelist entries needed
- [x] 2.3 Keep `GetLimits` for existing callers but source from config

## 3. Admin Page

- [x] 3.1 `Pages/Admin/Storage.cshtml(.cs)` (RequireAdmin) — strategy radio (Local/S3/MinIO/Aliyun OSS), provider fields (endpoint, bucket, access key id, secret, region, path-style toggle; masked after save + clear-secret action), per-purpose limit editors
- [x] 3.2 Persist via `SystemConfigService.SetAsync`; show "重启后生效" banner after save
- [x] 3.3 Connectivity test handler: write/read/delete probe `storage-probe/{guid}`; surface success/error without echoing secrets
- [x] 3.4 Menu entry "存储设置" in the AdminOps group

## 4. Renditions & Transparency

- [x] 4.1 Confirm `MediaTranscoder` writes renditions through the active `IStorageProvider` (already injected) — no URL changes
- [x] 4.2 Smoke-test Local unchanged (upload/serve/delete/renditions)
- [x] 4.3 Smoke-test S3 (MinIO if available) upload/serve/delete/renditions; record any failure

## 5. Secrets

- [x] 5.1 Secrets stored server-side in `Settings`; masked (`••••`) in the UI after save; "清除密钥" action; never logged

## 6. Build & Verify

- [x] 6.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 6.2 HTTP smoke tests:
  - Admin opens `/Admin/Storage`, selects `Local`, saves → restart banner shown; uploads still work
  - Connectivity test on the saved backend → success (Local, then MinIO probe); failure path surfaces without secret echo
  - Set strategy to `MinIO` (real MinIO instance) → save → restart app → upload lands in the bucket, `/files` streams it, probe delete works
  - Raise `Storage.Limits.Avatar.MaxBytes` → larger avatar accepted; limit enforced (3 MB avatar rejected at the 2 MB limit)
  - Secret masked after save; clear-secret empties the field
  - No regression: `/Files` uploads still work on the active backend
