## 1. Dependencies

- [x] 1.1 Confirm `async-io-jobs` is merged (the async path is implemented on top of it)
- [x] 1.2 Confirm `question-types` is merged (the `QuestionType` enum drives validation)

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.QuestionIO` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Assessments`, `OpenLearning.Storage`, `OpenLearning.Notifications`, `OpenLearning.Jobs` (never `OpenLearning.Data`)
- [x] 2.2 Add `QuestionImportJob { Id, UserId, CourseId?, QuizId?, Mode (Append/UpdateOrAppend), FileKey, Status (Pending/Running/Success/Failed), TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }` + config
- [x] 2.3 Add `QuestionRowError { Id, JobId, RowIndex, Field, Message }` + config
- [x] 2.4 Add `Question.RowId` (nullable unique-per-owner) for stable re-imports — extend `QuestionConfiguration`
- [x] 2.5 EF migration `AddQuestionIO` via `dotnet ef migrations add AddQuestionIO --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.6 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. Service Layer

- [x] 3.1 Implement `QuestionImportService.ImportSyncAsync(file, ownerId, quizId, mode)` — returns `(successCount, errors[])`
- [x] 3.2 Implement `QuestionImportService.ImportAsync(file, ownerId, quizId, mode)` — writes the upload to storage, creates a `QuestionImportJob`, enqueues an `IJob` (provided by `async-io-jobs`)
- [x] 3.3 Implement `QuestionImportService.ProcessJobAsync(jobId)` invoked by `async-io-jobs` — parses, validates, persists correct rows, writes the error file
- [x] 3.4 Implement `QuestionExportService.ExportAsync(filters, ownerId)` returning a stream — uses ClosedXML SXSSF; respects the 5000-row sync ceiling
- [x] 3.5 Implement `QuestionTemplateService.GetTemplateBytes()` — generates the template with column headers and a sample row

## 4. Validation Rules

- [x] 4.1 Required: `QuestionType`, `Stem`, `CorrectAnswer` (when type is objective)
- [x] 4.2 `QuestionType` whitelist: `SingleChoice, MultipleChoice, TrueFalse, FillBlank, ShortAnswer, FileUpload`
- [x] 4.3 SingleChoice / MultipleChoice require Options A–D (at least 2)
- [x] 4.4 TrueFalse requires `CorrectAnswer ∈ {True, False}`
- [x] 4.5 Difficulty ∈ `{Easy, Medium, Hard}` (free text accepted, mapped case-insensitively; unknown values reported)
- [x] 4.6 KnowledgeTag: max 200 chars, optional
- [x] 4.7 UpdateOrAppend mode: rows with `QuestionId` update; ownership checked at persistence time

## 5. Pages

- [x] 5.1 `Pages/Courses/Quizzes/Import.cshtml(.cs)` — sync path with file upload, mode selector, preview of parsed rows; shows inline errors
- [x] 5.2 `Pages/Courses/Quizzes/ImportAsync.cshtml(.cs)` — submits the async path, shows job id, links to job status
- [x] 5.3 `Pages/Courses/Quizzes/Export.cshtml(.cs)` — filter form (type, difficulty, tag) + Export button
- [x] 5.4 `Pages/Courses/Quizzes/ImportJobs.cshtml(.cs)` — list of recent import jobs with status and error-file link
- [x] 5.5 `Pages/Admin/QuestionBank/Import.cshtml(.cs)` and `Export.cshtml(.cs)` — bank equivalents with `IsBank = true` and `BankTopic` column
- [x] 5.6 `Pages/QuestionIO/Template.cshtml(.cs)` — endpoint that streams the template `.xlsx`

## 6. Rate Limit

- [x] 6.1 Implement `QuestionImportRateLimiter` (per-user per-hour) reading `question.import.rateLimitPerHour` from system-config (default 5)
- [x] 6.2 On exceed, return 429 with a Retry-After header
- [x] 6.3 Admin override: a config flag `question.import.rateLimitOverrideUserIds` (CSV) bypasses the limit

## 7. File Validation

- [x] 7.1 Accept only `.xlsx` (extension + content-type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)
- [x] 7.2 Max size 10 MB (config: `question.import.maxBytes`)
- [x] 7.3 Reject before any parsing; do not store rejected files

## 8. Notifications

- [x] 8.1 Send `import.completed` with success count + error-file link when an async import finishes
- [x] 8.2 Send `import.failed` with error summary when an async import crashes
- [x] 8.3 Send `export.ready` with download link when an async export file is written
- [x] 8.4 All three event types are added in `notification-events-extensions`; this change just emits them

## 9. Build & Verify

- [x] 9.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 9.2 HTTP smoke tests:
  - Upload 50 valid rows to a quiz the Instructor owns → 50 questions created
  - Upload 50 rows with 8 invalid → 42 created, error file lists the 8 with row numbers and reasons
  - Upload a 1500-row file → async job id returned; wait for completion; verify `import.completed` notification + error file (none expected) is delivered
  - Upload `.xls` / `.csv` / `.zip` → 400
  - Upload a 12 MB file → 400 (size rejected before storage)
  - Instructor B attempts to import into Instructor A's quiz → 403
  - UpdateOrAppend with a `QuestionId` owned by Instructor A → row reported as `not owner`
  - Submit 6 imports within an hour → 6th returns 429
  - Export filtered by type=difficulty → only matching rows; only owned questions
  - Export >5000 rows → goes async; `export.ready` notification delivered
  - Bank import by an Admin → rows created with `IsBank = true`
  - Bank import by a non-admin → 403
