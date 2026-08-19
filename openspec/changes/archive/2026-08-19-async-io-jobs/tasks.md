## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.AsyncIO` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Storage`, `OpenLearning.Notifications`, `OpenLearning.Jobs`, `OpenLearning.Logging` (never `OpenLearning.Data`)
- [x] 1.2 Add `AsyncIOJob { Id, UserId, Kind, FileKey, ResultFileKey?, Status (Pending/Running/Success/Failed), TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, FiltersJson?, StartedAt?, FinishedAt?, CreatedAt }` + config
- [x] 1.3 Add `AsyncIORowError { Id, JobId, RowIndex, Field, Message }` + config
- [x] 1.4 Define `IIOFileValidator` interface: `string[] AllowedExtensions { get; } long MaxBytes { get; } Task ValidateAsync(IFormFile file);`
- [x] 1.5 EF migration `AddAsyncIO` via `dotnet ef migrations add AddAsyncIO --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 1.6 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 2. Service Layer

- [x] 2.1 Implement `AsyncIOService.SubmitAsync(kind, file, ownerId, filtersJson, validator)`:
  - Run validator (rejects with 400 on extension/size failure)
  - Store the file via `IStorageProvider.SaveAsync`
  - Create `AsyncIOJob { Status = Pending }`
  - Resolve the `IJob` by `Kind` (e.g. `QuestionImport`) via DI and enqueue it (via `job-scheduler` API)
  - Return the job id
- [x] 2.2 Implement `AsyncIOService.GetJobAsync(id, ownerId)` — returns the job if owned by the user (admins can fetch any)
- [x] 2.3 Implement `AsyncIOService.ListJobsAsync(ownerId, kind?, status?, page, pageSize)`
- [x] 2.4 Implement `AsyncIOService.WriteResultAsync(jobId, stream)` and `WriteErrorFileAsync(jobId, errors)` — write to storage, set the file keys, send notifications
- [x] 2.5 Implement `AsyncIOJobDispatcher : IJob` that resolves the consumer's `IJob` by `Kind` and delegates to it

## 3. Pages

- [x] 3.1 `Pages/AsyncIO/Index.cshtml(.cs)` — list of the current user's jobs with filter (kind, status)
- [x] 3.2 `Pages/AsyncIO/Detail.cshtml(.cs)` — job status, counts, error-file download, result-file download
- [x] 3.3 Admin view: `Pages/Admin/AsyncIO/Index.cshtml(.cs)` — all jobs across users with filter (owner, kind)

## 4. Retention

- [x] 4.1 Register `IJob` named `async-io.cleanup` (in `scheduled-business-jobs`) that deletes files older than `asyncio.retention.days` and sets `ResultFileKey = null`, `ErrorFileKey = null`

## 5. Audit

- [x] 5.1 Write `OperationLog` row per finished job via `LogService`

## 6. Build & Verify

- [x] 6.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 6.2 Smoke tests:
  - Submit a fake import job; verify it appears in the user's list
  - Mark the job's file `old`; run `async-io.cleanup`; verify the file is removed and the key is nulled
  - Admin can list all jobs; user cannot see another user's job
  - Validator rejects `.csv` → 400
  - Validator rejects oversize file → 400
  - Failure path: throw inside the consumer's `IJob`; verify `Status = Failed` and `import.failed` notification
  - Idempotency: enqueue the same `Kind + FiltersJson + minute` twice; only one runs (per `job-scheduler`'s `IdempotencyKey`)