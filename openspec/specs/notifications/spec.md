# notifications Specification

## Purpose
TBD - created by archiving change notifications. Update Purpose after archive.
## Requirements
### Requirement: User has a notification inbox

The system SHALL provide an in-app notification inbox per user with read/unread state, and SHALL create notifications for events: new lesson in an enrolled course, quiz score published, certificate earned, course announcement, and instructor application outcome.

#### Scenario: Event creates notification
- **WHEN** an event affecting a user occurs (new lesson, quiz score, certificate, announcement, application outcome)
- **THEN** a notification with title, body, and a link is created for that user

#### Scenario: Mark as read
- **WHEN** a user opens or marks a notification read
- **THEN** the notification is shown as read and the unread count decreases

#### Scenario: Unread badge
- **WHEN** a user loads a page
- **THEN** the navigation shows the number of unread notifications

### Requirement: Instructor can post course announcements

The system SHALL allow the course owner to post an announcement that notifies all enrolled students.

#### Scenario: Post announcement
- **WHEN** the owning Instructor posts a course announcement
- **THEN** every enrolled student receives a notification linking to the announcement

#### Scenario: Non-owner cannot announce
- **WHEN** an Instructor who does not own the course attempts to post an announcement
- **THEN** the system SHALL deny access

### Requirement: Notifications may be delivered by email

The system SHALL send notification emails when an email provider is configured, without blocking in-app delivery.

#### Scenario: Email enabled
- **WHEN** an email provider is configured and an event occurs
- **THEN** an email is attempted in addition to the in-app notification

#### Scenario: Email disabled
- **WHEN** no email provider is configured
- **THEN** in-app notifications still work and email sending is skipped

### Requirement: Unread badge is rendered in the sidebar

The system SHALL surface the user's unread notification count as a numeric badge on the Notifications entry in the sidebar. On viewports where the sidebar is collapsed to icons, the badge SHALL additionally (or alternatively) appear on the top-bar notifications icon.

#### Scenario: Sidebar unread badge

- **WHEN** a user has unread notifications
- **THEN** the sidebar Notifications item shows a badge with the unread count

#### Scenario: Top-bar unread badge on narrow viewport

- **WHEN** the sidebar is collapsed and the user has unread notifications
- **THEN** the top-bar notifications icon shows the unread count

#### Scenario: No badge when zero

- **WHEN** the unread count is zero
- **THEN** neither badge is rendered

