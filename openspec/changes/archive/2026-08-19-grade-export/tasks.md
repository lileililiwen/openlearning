## 1. Dependencies

- [x] 1.1 Confirm `async-io-jobs` is merged
- [x] 1.2 Confirm `exams` change (pending) is merged so `ExamService.ListExamAttemptsForExportAsync` exists

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.GradeExport` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Assessments`, `OpenLearning.Assignments`, `OpenLearning.Enrollment`, `OpenLearning.Classes`, `OpenLearning.Storage`, `OpenLearning.Notifications`, `OpenLearning.Jobs` (never `OpenLearning.Data`)
- [x] 2.2 Add `GradeExportJob { Id, UserId, Kind (Submissions/Attempts/ExamAttempts/CourseRoster), FiltersJson, FileKey, Status, RowCount, CreatedAt, FinishedAt? }` + config
- [x] 2.3 EF migration `AddGradeExport` via `dotnet ef migrations add AddGradeExport --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.4 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. Service Layer

- [x] 3.1 Implement `GradeExportService.ExportSubmissionsAsync(filters, ownerId)` returning a stream (sync)
- [x] 3.2 Implement `GradeExportService.ExportQuizAttemptsAsync(filters, ownerId)` returning a stream
- [x] 3.3 Implement `GradeExportService.ExportExamAttemptsAsync(filters, ownerId)` returning a stream
- [x] 3.4 Implement `GradeExportService.ExportCourseRosterAsync(filters, ownerId)` returning a stream
- [x] 3.5 Implement `GradeExportService.SubmitAsync(...)` for async path — creates a `GradeExportJob` and enqueues an `IJob` via `async-io-jobs`
- [x] 3.6 Implement `GradeExportService.ProcessJobAsync(jobId)` invoked by the job — runs the export, writes to storage, emits `export.ready` notification
- [x] 3.7 Implement ownership / TA scoping using the existing policy attributes + `IClassAssignmentLookup`

## 4. Streaming

- [x] 4.1 Use ClosedXML SXSSFWorkbook for writes
- [x] 4.2 Iterate the source query with keyset paging (`WHERE Id > @last ORDER BY Id LIMIT @batch`)
- [x] 4.3 Flush every 1000 rows

## 5. Pages

- [x] 5.1 `Pages/Courses/Assignments/Export.cshtml(.cs)` — Instructor-only; filter by date range + status
- [x] 5.2 `Pages/Courses/Quizzes/ExportResults.cshtml(.cs)` — per-quiz and per-course
- [x] 5.3 `Pages/Courses/Exams/ExportResults.cshtml(.cs)` — per-exam
- [x] 5.4 `Pages/Courses/Roster/Export.cshtml(.cs)` — Instructor / Admin
- [x] 5.5 `Pages/TA/Class/Export.cshtml(.cs)` — TA only; restricted to assigned classes
- [x] 5.6 `Pages/GradeExport/Jobs.cshtml(.cs)` — list of recent export jobs with download link and retention expiry

## 6. Notifications

- [x] 6.1 Send `export.ready` with download link when an async export finishes
- [x] 6.2 Send `export.progress` at 25 / 50 / 75% for jobs > 5 minutes
- [x] 6.3 Event types added in `notification-events-extensions`

## 7. File Retention

- [x] 7.1 Register `IJob` named `grade.export.cleanup` (in `scheduled-business-jobs`) that deletes files older than `grade.export.retentionDays` and marks the `GradeExportJob.FileKey = null`

## 8. Build & Verify

- [x] 8.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 8.2 HTTP smoke tests:
  - Owner exports 50 submissions → 50 rows downloaded
  - Owner exports with date filter → only matching rows
  - Non-owner denied
  - 5000-row export goes async; `export.ready` notification delivered; download works
  - Memory stays under 100 MB on a 50,000-row smoke-test export (manual)
  - TA denied for an unassigned class
  - Expired link returns 404
  - Operation log entry exists