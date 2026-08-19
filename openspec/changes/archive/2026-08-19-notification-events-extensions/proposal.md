## Why

The existing `notifications` capability covers event types for new lesson, quiz score, certificate, course announcement, instructor application outcome, and membership expiry. The brief calls out additional events the system must emit: 作业批改完成 (assignment graded), 考试提醒 (exam reminder), 上课前30分钟提醒 (class / live start reminder), 作业截止提醒 (assignment due), and the 时间-driven events from `scheduled-business-jobs` (expiry-soon, expiry, due-soon, due-missed, refund timeout, order expired). Today none of these are emitted. We add the missing event types and the model fields they need (e.g. `Notification.ClassGroupId` for class-scoped announcements).

## What Changes

- Extend `notifications` with the additional event types, delivery channels, and per-type preferences (`account-settings`).
- Add `Notification.ClassGroupId` (nullable) for class-scoped announcements; `notification-events-extensions` covers the schema change.
- Bind the new event types to their owning modules' service methods so a single change in the module flows a notification without the dispatcher needing to know module internals.
- Add the matching admin-templates entries so each new event has an editable template via `system-config`.

## Capabilities

### New Capabilities

- `notification-events-extensions`: the additional event types, their templates, their per-type preferences, their delivery semantics, and the binding points in the owning modules.

### Modified Capabilities

- `notifications`: ADDED requirements for the new event types and the `ClassGroupId` column.
- `assignments`: emits `assignment.graded` when an instructor grades a submission.
- `exams` (pending): emits `exam.starting-soon` (driven by `scheduled-business-jobs`).
- `class-groups` (proposed): supports class-scoped announcement delivery via `Notification.ClassGroupId`.
- `course-access-period` (proposed): emits `enrollment.expiring-soon` and `enrollment.expired`.
- `commerce-extras`: emits `refund.timeout-rejected` (driven by `scheduled-business-jobs`) and `invoice.*` (from `invoice-management`).
- `account-settings`: each new event type has a per-channel preference toggle.
- `async-io-jobs` (proposed): emits `import.completed`, `import.failed`, `export.ready`, `export.progress` for bulk import/export jobs.
- `student-bulk-import` (proposed): emits `account.welcome` and `enrollment.granted-bulk` for newly created / newly enrolled accounts.

## Impact

- Migration `AddNotificationExtensions` adds `Notification.ClassGroupId` (nullable FK) and seeds templates for the new types in `notification-template` config.
- No new module; the changes are in the existing `OpenLearning.Notifications` module and the existing `system-config` defaults.
- Each owning module gains a single call site to the notification service (or has a binding registered via DI if a hard reference would create a cycle).