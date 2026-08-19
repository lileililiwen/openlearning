## 1. Dependencies

- [x] 1.1 Confirm `async-io-jobs` is merged

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.CourseOutlineIO` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.CourseManagement`, `OpenLearning.Storage`, `OpenLearning.Jobs` (never `OpenLearning.Data`)
- [x] 2.2 Add `OutlineImportJob { Id, UserId, CourseId, Mode, FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }` + config
- [x] 2.3 Add `OutlineRowError { Id, JobId, RowIndex, Field, Message }` + config
- [x] 2.4 EF migration `AddCourseOutlineIO` via `dotnet ef migrations add AddCourseOutlineIO --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.5 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. Service Layer

- [x] 3.1 Implement `OutlineImportService.ImportSyncAsync(file, ownerId, courseId, mode)` returning `(successCount, errors[])`
- [x] 3.2 Implement `OutlineImportService.ImportAsync(file, ownerId, courseId, mode)` — creates a `OutlineImportJob` and enqueues an `IJob` via `async-io-jobs`
- [x] 3.3 Implement `OutlineImportService.ProcessJobAsync(jobId)` — parses, validates, persists modules/lessons, writes the error file
- [x] 3.4 Implement `OutlineImportService.PreflightReplaceAsync(courseId)` returning the count of modules, lessons, and orphan quizzes that will be deleted
- [x] 3.5 Implement `OutlineExportService.ExportAsync(courseId, ownerId)` returning a stream (SXSSF)
- [x] 3.6 Implement `OutlineTemplateService.GetTemplateBytes()` returning the template bytes

## 4. Validation Rules

- [x] 4.1 `ModuleTitle` non-empty
- [x] 4.2 `LessonTitle` non-empty when the row is a lesson
- [x] 4.3 `ModuleOrder`, `LessonOrder` ≥ 0
- [x] 4.4 No duplicate `(ModuleOrder, LessonOrder)` within the same module
- [x] 4.5 `LessonContentUrl` length ≤ 2000 chars (text reference only)

## 5. Pages

- [x] 5.1 `Pages/Courses/Outline/Import.cshtml(.cs)` — file upload, mode selector, pre-flight summary for Replace, inline error preview
- [x] 5.2 `Pages/Courses/Outline/ImportJobs.cshtml(.cs)` — recent jobs with status and error-file links
- [x] 5.3 `Pages/Courses/Outline/Export.cshtml(.cs)` — download
- [x] 5.4 `Pages/CourseOutlineIO/Template.cshtml(.cs)` — streams the template

## 6. Replace Mode Safety

- [x] 6.1 `OutlineImportService.PreflightReplaceAsync` returns counts so the page can show a confirmation prompt
- [x] 6.2 Replace wipes modules + lessons; quizzes / assignments attached to lessons are detached (lesson id becomes orphaned) and listed in the confirmation
- [x] 6.3 An operation-log entry records the wipe

## 7. File Safety

- [x] 7.1 Accept only `.xlsx`, max 5 MB (config: `courseOutline.import.maxBytes`)

## 8. Notifications

- [x] 8.1 Send `import.completed` and `import.failed` (added in `notification-events-extensions`)
- [x] 8.2 Send `export.ready` for async exports (added in `notification-events-extensions`)

## 9. Audit

- [x] 9.1 Write `OperationLog` row per finished import job

## 10. Build & Verify

- [x] 10.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 10.2 HTTP smoke tests:
  - Owner imports 100 valid rows → 100 modules/lessons created
  - Owner imports 100 rows with 7 invalid → 93 created, error file lists 7
  - Owner imports 1500 rows → async job id; `import.completed` notification delivered
  - Owner selects Replace → pre-flight summary shown, confirmation required; submitting wipes outline
  - Non-owner Instructor denied
  - TA denied
  - Admin can import into any course
  - Owner exports the outline → `.xlsx` downloads with the right ordering
  - `.csv` rejected with 400
  - 7 MB file rejected with 400