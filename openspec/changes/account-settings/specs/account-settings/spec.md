## ADDED Requirements

### Requirement: User can verify their real name

The system SHALL allow a user to submit real-name information and SHALL allow an Admin to review and approve or reject it.

#### Scenario: Submit verification
- **WHEN** a user submits real-name information
- **THEN** the identity status becomes pending and the Admin is notified

#### Scenario: Approve or reject
- **WHEN** an Admin approves or rejects a pending verification
- **THEN** the status is updated, the user is notified, and the status is shown on their profile

#### Scenario: Unverified instructor cannot publish
- **WHEN** an Instructor whose identity is not verified tries to publish a course
- **THEN** publishing is denied with a message directing them to verify

### Requirement: User controls notification delivery

The system SHALL allow a user to enable or disable in-app and email delivery per notification type.

#### Scenario: Disable a type
- **WHEN** a user disables email for a notification type
- **THEN** email for that type is skipped while in-app delivery follows its own toggle

#### Scenario: Preferences respected
- **WHEN** an event occurs for a user with delivery disabled
- **THEN** no notification of that type is delivered through the disabled channel
