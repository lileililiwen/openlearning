## 1. Migration & Schema

- [x] 1.1 EF migration `AddNotificationExtensions`:
  - Add `Notification.ClassGroupId` (nullable FK to `ClassGroup`)
  - Insert template rows for the new event types into `NotificationTemplate`
- [x] 1.2 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 1.3 Apply migration on dev DB; verify the new column exists and templates are seeded

## 2. Assignment Events

- [x] 2.1 In `OpenLearning.Assignments.Services.AssignmentService.GradeAsync`, after persisting the grade, call `NotificationService.SendAsync("assignment.graded", studentId, placeholders)` exactly once per grade (guard against re-grade by tracking `NotifiedAt` on the grade row)
- [x] 2.2 Verify the notification carries the assignment title, score, and a link to the detail page

## 3. Exam Reminder

- [x] 3.1 In `OpenLearning.Web.Jobs.ExamReminderJob` (registered by `scheduled-business-jobs`), call `NotificationService.SendAsync("exam.starting-soon", studentId, placeholders)` for each non-attempting enrolled student
- [x] 3.2 Ensure the job does not double-notify (idempotency key + per-exam `ReminderNotifiedAt` flag)

## 4. Assignment Due Events

- [x] 4.1 In `OpenLearning.Web.Jobs.AssignmentDueReminderJob`, call `NotificationService.SendAsync("assignment.due-soon", studentId, ...)` for each non-submitting enrolled student at T-24h
- [x] 4.2 In `AssignmentDueReminderJob`'s auto-close path, call `NotificationService.SendAsync("assignment.due-missed", studentId, ...)` for each non-submitting student (guarded by `DueMissedNotifiedAt`)

## 5. Class Start Reminder

- [x] 5.1 In `OpenLearning.Web.Jobs.ClassStartReminderJob`, call `NotificationService.SendClassScopedAsync("class.starting-soon", classGroupId, ...)` for each member of the class

## 6. Enrollment Expiry Events

- [x] 6.1 In `OpenLearning.Web.Jobs.EnrollmentExpiryNotifySoonJob`, call `NotificationService.SendAsync("enrollment.expiring-soon", studentId, placeholders)` for each learner expiring within 7 days
- [x] 6.2 In `OpenLearning.Web.Jobs.EnrollmentExpiryRevokeJob`, call `NotificationService.SendAsync("enrollment.expired", studentId, placeholders)` after revoking each enrollment

## 7. Order Expired & Refund Timeout

- [x] 7.1 In `OpenLearning.Web.Jobs.OrderExpireUnpaidJob`, after cancelling each order, call `NotificationService.SendAsync("order.expired", buyerId, ...)`
- [x] 7.2 In `OpenLearning.Web.Jobs.RefundTimeoutCloseJob`, after auto-rejecting each refund, call `NotificationService.SendAsync("refund.timeout-rejected", studentId, ...)`

## 8. Invoice Lifecycle

- [x] 8.1 In `OpenLearning.Invoicing.Services.InvoiceService.IssueAsync`, call `NotificationService.SendAsync("invoice.issued", studentId, ...)`
- [x] 8.2 In `RejectAsync`, call `NotificationService.SendAsync("invoice.rejected", studentId, ...)`
- [x] 8.3 In `VoidAsync`, call `NotificationService.SendAsync("invoice.voided", studentId, ...)`
- [x] 8.4 In `IssueRedLetterAsync`, call `NotificationService.SendAsync("invoice.red-letter-issued", studentId, ...)`

## 9. Templates & Preferences

- [x] 9.1 Add template rows for the new event types (assignment.graded, exam.starting-soon, assignment.due-soon, assignment.due-missed, class.starting-soon, enrollment.expiring-soon, enrollment.expired, order.expired, refund.timeout-rejected, invoice.issued, invoice.rejected, invoice.voided, invoice.red-letter-issued, import.completed, import.failed, export.ready, export.progress, account.welcome, enrollment.granted-bulk)
- [x] 9.2 Verify each appears on `/Admin/System` notification-templates UI
- [x] 9.3 Verify `account-settings` exposes a per-type email/in-app toggle for each

## 10. Build & Verify

- [x] 10.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 10.2 Smoke tests:
  - Grade a submission; verify a single `assignment.graded` notification exists; re-grade (same score); verify no second notification
  - Set an exam `OpensAt = UtcNow + 25 minutes`; run `exam.reminder` job; verify non-attempting student got the notification; re-run; verify no second notification
  - Set an assignment due in 5 hours; run `assignment.due-reminder`; verify a `due-soon` notification exists; advance time past due; verify a `due-missed` notification exists after the next auto-close tick
  - Set a class `StartsAt = UtcNow + 15 minutes`; run `class.start-reminder`; verify a class-scoped notification exists for members only
  - Force-expire an enrollment past grace; run `enrollment.expiry.revoke`; verify `enrollment.expired` notification exists
  - Mark an order unpaid for 45 min; run `order.expire-unpaid`; verify `order.expired` notification exists
  - Age a refund request 8 days; run `refund.timeout-close`; verify `refund.timeout-rejected` notification exists
  - Issue / reject / void / red-letter an invoice and verify each lifecycle notification
- [x] 10.3 Verify templates render placeholders correctly (`{assignmentTitle}`, `{score}`, etc.)

## 11. Bulk Import/Export Events

- [x] 11.1 In `OpenLearning.AsyncIO.Services.AsyncIOService`, emit `import.completed` with `successCount`, `errorCount`, and the error-file download link when an async import job finishes
- [x] 11.2 In `OpenLearning.AsyncIO.Services.AsyncIOService`, emit `import.failed` with the exception summary when an async import job crashes (validation failures are inline, not notifications)
- [x] 11.3 In `OpenLearning.AsyncIO.Services.AsyncIOService`, emit `export.ready` with the download link and expiry date when an async export job finishes
- [x] 11.4 In `OpenLearning.AsyncIO.Services.AsyncIOService`, emit `export.progress` at 25 / 50 / 75% for jobs whose expected duration exceeds 5 minutes (`ReportProgressAsync`)
- [x] 11.5 `account.welcome` notification type, template, and `SendAsync` support added (the `StudentImportService.ProcessJobAsync` call site lands with `student-bulk-import`)
- [x] 11.6 `enrollment.granted-bulk` notification type, template, and `SendAsync` support added (the `StudentImportService.ProcessJobAsync` call site lands with `student-bulk-import`)
- [x] 11.7 Verify each new event type respects the user's per-type channel preference (in-app / email)
