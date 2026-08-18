# messaging-channels Specification

## Purpose
TBD - created by archiving change messaging-channels. Update Purpose after archive.
## Requirements
### Requirement: Notifications can be sent over multiple channels

The system SHALL deliver notifications over in-app, email, SMS, and web-push channels according to configuration and the user's preferences.

#### Scenario: Channel dispatch
- **WHEN** an event creates a notification
- **THEN** every enabled channel matching the user's preferences is attempted

#### Scenario: Disabled channel
- **WHEN** a channel is disabled by config
- **THEN** delivery over that channel is skipped

#### Scenario: Channel failure
- **WHEN** a channel send fails
- **THEN** in-app delivery is unaffected and the error does not surface to the user

### Requirement: Users can receive web push

The system SHALL allow a user to subscribe a browser to web push and SHALL deliver notifications to their subscriptions.

#### Scenario: Subscribe
- **WHEN** a user grants permission and subscribes
- **THEN** the subscription is stored for the user

#### Scenario: Push delivery
- **WHEN** a notification event occurs for a user with subscriptions
- **THEN** a push notification is attempted for each stored subscription

