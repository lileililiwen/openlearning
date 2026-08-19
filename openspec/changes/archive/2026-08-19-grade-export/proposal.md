## Why

Instructors must produce paper / archival copies of student work for offline grading, parent meetings, accreditation, and end-of-term reports. The brief flags this as P0. The platform records attempts, submissions, scores, and per-question breakdowns but offers no batch export.

## What Changes

- Provide Excel / `.xlsx` exports for: assignment submissions (with grades), quiz attempts (with answers + scores), exam attempts (with answers + scores), and final course-grade rosters.
- Sync for ≤1000 rows; async (via `async-io-jobs`) for larger.
- Streamed writes via SXSSF.
- Ownership-scoped: an Instructor can only export for their own courses; an Admin / TA can export for any course / class they own.
- Filter by course / quiz / exam / class group / date range / status.
- No import counterpart — answer data cannot be externally authored.

## Capabilities

### New Capabilities

- `grade-export`: streaming Excel export of submissions, attempts, and course-grade rosters.

### Modified Capabilities

- `assignments`: `AssignmentService` exposes `ListSubmissionsForExportAsync(filters)` returning a paged query.
- `assessments`: `AttemptService` exposes `ListAttemptsForExportAsync(filters)`.
- `exams` (pending): `ExamService` exposes `ListExamAttemptsForExportAsync(filters)`.
- `ta-and-finance-roles`: TA export is restricted to assigned classes via `IClassAssignmentLookup`.

## Impact

- New `OpenLearning.GradeExport` module: `GradeExportJob { Id, UserId (exporter), Kind (Submissions/Attempts/ExamAttempts/CourseRoster), FiltersJson, FileKey, Status, RowCount, CreatedAt, FinishedAt? }`. EF migration `AddGradeExport`.
- Services: `GradeExportService.ExportAsync` (sync stream), `GradeExportService.SubmitAsync` (async job), `GradeExportService.ProcessJobAsync`.
- Pages: `Pages/Courses/Assignments/Export.cshtml(.cs)`, `Pages/Courses/Quizzes/ExportResults.cshtml(.cs)` (reuses `assessments`), `Pages/Courses/Exams/ExportResults.cshtml(.cs)`, `Pages/Courses/Roster/Export.cshtml(.cs)` (course-level roster with final grade). TA-scoped roster export at `/TA/Class/{id}/Export`.
- One-line DI: `builder.Services.AddGradeExportModule();`.