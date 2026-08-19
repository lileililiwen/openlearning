## 1. Dependencies

- [x] 1.1 Confirm `async-io-jobs` is merged
- [x] 1.2 Confirm `class-groups` (or its `IClassAssignmentLookup` stub from `ta-and-finance-roles`) is in place

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.StudentIO` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Enrollment`, `OpenLearning.Classes`, `OpenLearning.Storage`, `OpenLearning.Notifications`, `OpenLearning.Jobs` (never `OpenLearning.Data`)
- [x] 2.2 Add `StudentImportJob { Id, UserId (importer), Mode, FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }` + config
- [x] 2.3 Add `StudentImportRowError { Id, JobId, RowIndex, Field, Message }` + config
- [x] 2.4 EF migration `AddStudentIO` via `dotnet ef migrations add AddStudentIO --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.5 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. Service Layer

- [x] 3.1 Implement `StudentImportService.ImportSyncAsync(file, importerId, scope)` returning `(successCount, errors[])`
- [x] 3.2 Implement `StudentImportService.ImportAsync(file, importerId, scope)` — writes the upload, creates a `StudentImportJob`, enqueues an `IJob` via `async-io-jobs`
- [x] 3.3 Implement `StudentImportService.ProcessJobAsync(jobId)` invoked by the job — parses, validates, creates accounts, enrolls, accumulates errors, writes the error file
- [x] 3.4 Implement `StudentImportService.GenerateWelcomeTokenAsync(userId)` returning a one-time reset token
- [x] 3.5 Implement `StudentImportTemplateService.GetTemplateBytes()` — generates the `.xlsx` template

## 4. Validation Rules

- [x] 4.1 `Action ∈ {Create, CreateAndEnroll, EnrollExisting}`
- [x] 4.2 `Email` valid format (RFC 5322 simplified) when present
- [x] 4.3 `Password` ≥ 8 chars and meets Identity policy when supplied
- [x] 4.4 Email uniqueness per existing user and per row
- [x] 4.5 `CourseIds` parseable to integer list; non-existent course ids reported
- [x] 4.6 `ClassGroupIds` validated against `IClassAssignmentLookup` for TA imports
- [x] 4.7 Paid course without order → row error `course requires purchase`; account still created

## 5. Pages

- [x] 5.1 `Pages/Admin/Students/Import.cshtml(.cs)` — Admin/Finance; file upload, mode selector, sync vs async, inline error preview
- [x] 5.2 `Pages/Admin/Students/ImportJobs.cshtml(.cs)` — recent jobs with success/error counts and error-file links
- [x] 5.3 `Pages/Admin/Students/Template.cshtml(.cs)` — endpoint that streams the template
- [x] 5.4 `Pages/TA/Class/Import.cshtml(.cs)` — TA-scoped; only EnrollExisting and CreateAndEnroll with ClassGroupIds matching the current class are accepted

## 6. Notifications

- [x] 6.1 Send `account.welcome` to each created user with display name + (optional) reset link
- [x] 6.2 Send `enrollment.granted-bulk` to existing users newly enrolled
- [x] 6.3 Send `import.completed` to the importer with success/error counts and error-file link
- [x] 6.4 Event types added in `notification-events-extensions`; this change just emits

## 7. Audit

- [x] 7.1 Write `OperationLog` row per finished import job (importer, file key, success count, error count)

## 8. Build & Verify

- [x] 8.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 8.2 HTTP smoke tests:
  - Admin uploads 50 valid `Create` rows → 50 accounts, 0 errors, 50 welcome notifications
  - Admin uploads 50 rows, 5 with duplicate emails → 45 accounts created, 5 errors with `duplicate email`
  - Admin uploads `CreateAndEnroll` rows referencing a paid course → accounts created; enrollment rows reported as errors
  - Admin uploads 1500 rows → async job id; wait for completion; `import.completed` notification delivered with error file (none expected if all valid)
  - Admin uploads `.csv` → 400
  - TA uploads at `/TA/Class/{id}/Import` with another class id → row error `class not assigned`
  - Student denied the import endpoint → 403
  - Welcome link works: clicking resets the password and signs the user in
  - Audit log entry visible in `/Admin/Logs/Operations`
