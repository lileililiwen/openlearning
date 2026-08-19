## Context

`notifications` already covers five event types. The brief requires roughly twice that. Each event type is a small contract: a unique key, a template id, the recipient rule, and the channels. The dispatcher (`NotificationService`) is generic, so adding types is mostly declarative. The schema needs one new nullable column (`ClassGroupId`) for class scoping.

Per-type preferences and editable templates already exist via `account-settings` and `system-config`; we just need to seed defaults for the new types and ensure the UI surfaces them.

## Goals / Non-Goals

**Goals:**
- Define and wire the missing event types.
- Add class-scoping to `Notification`.
- Seed templates for each new type.
- Honour per-type preferences and templates.

**Non-Goals:**
- New delivery channels (existing in-app / email / SMS / web-push are reused).
- Aggregation rules (digest emails) — out of scope.
- Notification UI redesign.

## Decisions

- **One migration, `AddNotificationExtensions`**, that adds `Notification.ClassGroupId` and inserts `NotificationTemplate` rows for the new event types into the system-config store.
- **Template key naming** follows `<entity>.<verb>` convention (`assignment.graded`, `exam.starting-soon`, etc.).
- **Recipient resolution** for class-scoped notifications joins on `ClassAssignment` (from `class-groups`).
- **Notifications stay generic**: dispatcher reads templates and preferences; owning modules emit by calling `NotificationService.SendAsync(key, recipientId, placeholders)`.

## Risks / Trade-offs

- [Risk: too many notifications fired at once during a scheduled tick] → Mitigation: per-recipient rate limit at the dispatcher; covered by existing `messaging-channels` throttling.
- [Risk: a deleted template silently disables notifications] → Mitigation: the dispatcher falls back to caller-provided text when no template exists (existing behaviour).
- [Risk: class-scoped notifications reach former students if revocation is racy] → Mitigation: the recipient query joins on `RevokedAt IS NULL` (per `course-access-period`).

## Migration Plan

1. EF migration `AddNotificationExtensions` adds the column and seeds templates.
2. Each owning module gains a call site at the existing mutation point (no new module).
3. Verify each event fires from the relevant trigger (UI action or scheduled job).

## Open Questions

- Should we allow admins to disable specific event types globally (kill switch)? Out of scope here; the existing `account-settings` per-type preference covers it for users, and admins can pause the relevant scheduled job via `job-scheduler`.