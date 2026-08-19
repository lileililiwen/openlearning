## Why

The brief mandates that bulk import/export of questions, students, grades, and outlines must NOT run on a synchronous HTTP request — uploading a 5000-row file or exporting 50,000 rows would time out and risk OOM. Today we have no shared async IO framework: each module would have to reinvent upload storage, status tracking, error-file generation, and notification wiring. We add a single `OpenLearning.AsyncIO` module that all IO changes (`question-import-export`, `student-bulk-import`, `grade-export`, `course-outline-import-export`, `coupon-bulk-import`) plug into.

## What Changes

- Generic async IO job framework: an upload is stored, validated for type / size, persisted as an `AsyncIOJob`, and processed by an `IJob` (per `job-scheduler`).
- Result file (success artifact or error file) is written back to storage and the user is notified via `notifications`.
- File retention policy: configurable per job type (default 7 days) — older files are pruned by `scheduled-business-jobs`.
- Common validation: file extension, content-type, max size — each consumer can layer its own validation on top.
- Job visibility: every async IO job shows up on the existing admin Jobs page (`/Admin/Jobs` from `job-scheduler`).

## Capabilities

### New Capabilities

- `async-io-jobs`: shared async IO substrate — file storage, status, error file, notification hooks, retention.

### Modified Capabilities

- `job-scheduler`: every `IJob` registered by an IO module is auto-listed on the admin Jobs page (no behaviour change, just confirmation).
- `notifications` (via `notification-events-extensions`): the `import.completed`, `import.failed`, `export.ready`, `export.progress` event types are added in `notification-events-extensions`.

## Impact

- New `OpenLearning.AsyncIO` class library: `AsyncIOJob { Id, UserId, Kind (QuestionImport / StudentImport / GradeExport / CourseOutlineImport / CouponImport / ...), FileKey, ResultFileKey?, Status (Pending / Running / Success / Failed), TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, FiltersJson?, StartedAt?, FinishedAt?, CreatedAt }`, `AsyncIORowError { Id, JobId, RowIndex, Field, Message }`. EF migration `AddAsyncIO`.
- Services: `AsyncIOService.SubmitAsync(kind, file, ownerId, filtersJson)` (validates file, stores, creates job, enqueues `IJob`), `AsyncIOService.GetJobAsync(id)`, `AsyncIOService.ListJobsAsync(ownerId)`.
- Pages: `Pages/AsyncIO/Index.cshtml(.cs)` — list of recent jobs for the current user (their own jobs only); `Pages/AsyncIO/Detail.cshtml(.cs)` — job status, error-file download.
- File storage uses `OpenLearning.Storage`; retention uses `scheduled-business-jobs`.
- One-line DI: `builder.Services.AddAsyncIOModule();`.