## ADDED Requirements

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