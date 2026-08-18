# Notifications — Design

## Context

The platform produces user-relevant events but has no delivery mechanism. This change adds an in-app notification inbox and course announcements, with email as an optional channel.

## Goals

- Every important event creates a notification the user can see and act on.
- Instructors can announce to enrolled students.
- Read/unread state is tracked per user.

## Non-Goals

- No push/web-push, SMS, or social channels.
- No notification preferences/config (all events notify; preferences deferred).
- Email requires external provider config; in-app delivery is the primary channel.

## Decisions

### D1: New `OpenLearning.Notifications` module
`Notification { Id, UserId, Type, Title, Body, Link, IsRead, CreatedAt }` (index on `(UserId, CreatedAt)`). `CourseAnnouncement { Id, CourseId, AuthorId, Body, CreatedAt }`. Services: `NotificationService.CreateAsync`, `GetRecentAsync(userId)`, `MarkReadAsync`, `UnreadCountAsync`; `AnnouncementService.PostAsync` (owner-only), `ListForCourseAsync`.

### D2: Event producers call the service
Progress (SCORM lesson completed → certificate), assessments (quiz score), user-management (application approved/rejected), and course announcements all call `NotificationService.CreateAsync`. To avoid cross-module service coupling, producers either depend on `NotificationService` (module-to-module, acyclic since Notifications references only Auth) or the Web layer raises notifications after domain calls. Decision: the Web layer raises notifications at the call sites (keeps domain modules decoupled); a thin `NotificationHub` notifier can be added later.

### D3: Bell UI
The layout renders a bell with unread count (polled on load) and a dropdown/`/Notifications` page. Marking read is a POST handler; clicking a notification follows its `Link`.

### D4: Email (optional)
An `IEmailSender` abstraction (placeholder no-op) is wired to an SMTP implementation only when `Email:Enabled` is set. Sending is fire-and-forget; failures never block the in-app notification.

## Risks / Trade-offs

- **Notification spam** → A single notification per event type per day for the noisy events (e.g., chat) is deduped by type+date.
- **Cross-module coupling** → Raising notifications from the Web layer keeps modules acyclic at the cost of a few extra lines per call site.

## Migration Plan

One migration creates `Notifications` and `CourseAnnouncements`.

## Open Questions

- Email templates and sender identity — deferred until an SMTP provider is configured.
