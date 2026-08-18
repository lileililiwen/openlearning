# live-chat Specification

## Purpose
TBD - created by archiving change live-chat. Update Purpose after archive.
## Requirements
### Requirement: Course has a chat room

The system SHALL provide a chat room per course where enrolled students and the course owner can read and post messages.

#### Scenario: Owner opens chat
- **WHEN** the course owner opens a course's chat
- **THEN** the chat page shows the recent message history and a message composer

#### Scenario: Enrolled student opens chat
- **WHEN** an enrolled Student opens the course's chat
- **THEN** the chat page shows the recent message history and a message composer

### Requirement: Messages are delivered in real time

The system SHALL broadcast new chat messages to all participants in the course's chat room as they are posted.

#### Scenario: Send a message
- **WHEN** an enrolled Student or the owner posts a message
- **THEN** the message is persisted and delivered in real time to everyone in the course chat room

#### Scenario: Non-participant cannot send
- **WHEN** a user who is neither enrolled in the course nor its owner attempts to post
- **THEN** the system SHALL reject the message

### Requirement: Chat history persists

The system SHALL persist chat messages so history survives page reloads and server restarts.

#### Scenario: Reload shows history
- **WHEN** a participant opens the chat page after messages have been posted
- **THEN** the recent messages are shown

