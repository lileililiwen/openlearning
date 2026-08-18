## Why

Events that matter to users (new lessons, quiz results, certificates, course announcements) are currently invisible unless a user happens to look. A notification inbox keeps users informed and drives them back into the platform.

## What Changes

- **In-app notifications**: an inbox/bell with read/unread state. Events: new lesson in an enrolled course, quiz score published, certificate earned, course announcement, instructor application outcome.
- **Course announcements**: instructors post announcements to enrolled students, which create notifications.
- Optional **email** delivery when an SMTP/email provider is configured (notifications are still delivered in-app without it).

## Capabilities

### New Capabilities
- `notifications`: in-app notification inbox, read state, event sources, course announcements, and optional email delivery.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Notifications` module: `Notification { Id, UserId, Type, Title, Body, Link, IsRead, CreatedAt }` and `CourseAnnouncement { Id, CourseId, AuthorId, Body, CreatedAt }`; `NotificationService` (create, list, mark-read) and `AnnouncementService` (post, list).
- The layout adds a notifications bell; a course edit page gains an announcement composer.
- Existing modules call `NotificationService` to raise events (progress, assessments, certificates, user-management).
