## Why

Institutional customers (培训机构, 高校, 企业内训) need to onboard tens to hundreds of students at once with one batch import — assigning pre-paid courses and bypassing individual email verification. Today the only path is one-by-one registration. The brief calls this out as P0 (must-have) for the institution scenario.

## What Changes

- Provide an Excel import surface that creates student accounts in bulk and optionally enrolls each student into one or more courses / class groups (per `class-groups`).
- Sync for ≤200 rows, async for larger (via `async-io-jobs`).
- Partial-success: correct rows commit, error rows reported.
- Three actions per row: `CreateAccount`, `CreateAccountAndEnroll`, `EnrollExisting` (the user already has an account, just enroll).
- Email uniqueness is enforced server-side; duplicate emails produce a row error.
- The system SHALL NOT bypass the existing `account-login-extras` phone-code flow — phone sign-in continues to work for accounts created via this import.
- A welcome notification is emitted for each successfully created account.

## Capabilities

### New Capabilities

- `student-bulk-import`: bulk student account creation, bulk enrollment, three row-action modes, partial-success reporting, ownership-scoped to Admin/Finance/TA.

### Modified Capabilities

- `user-management`: Admin user list gains a filter for "created via bulk import" and a link to the source import job.
- `enrollment`: bulk-enrollment uses the existing `EnrollmentService.EnrollAsync` per row; the new `ClassGroupId` (from `class-groups`) is supported when the row specifies a class group.
- `account-login-extras`: imported accounts can use phone-code sign-in if a phone is supplied; otherwise email/password is set with a one-time reset link.
- `notification-events-extensions` (proposed): receives new `account.welcome`, `enrollment.granted-bulk` events.

## Impact

- New `OpenLearning.StudentIO` module: `StudentImportJob { Id, UserId (importer), Mode (Create/Enroll/Mixed), FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }`, `StudentImportRowError { Id, JobId, RowIndex, Field, Message }`.
- EF migration `AddStudentIO` adds the two tables.
- Services: `StudentImportService.ImportSyncAsync`, `StudentImportService.ImportAsync`, `StudentImportService.ProcessJobAsync`.
- Pages: `Pages/Admin/Students/Import.cshtml(.cs)`, `Pages/Admin/Students/ImportJobs.cshtml(.cs)`; class-scoped equivalents under `/TA/Class/{id}/Import` (TA permitted).
- Reuses `IdentityUserManager.CreateAsync` for account creation; `EnrollmentService.EnrollAsync` for enrollment.
- One-line DI: `builder.Services.AddStudentIOModule();`.