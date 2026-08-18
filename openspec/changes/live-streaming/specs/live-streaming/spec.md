## ADDED Requirements

### Requirement: Instructor schedules live sessions

The system SHALL allow the course owner to create a live session with a title and start/end time, and SHALL show upcoming and ongoing sessions to enrolled students.

#### Scenario: Schedule session
- **WHEN** the owning Instructor creates a live session
- **THEN** enrolled students see it in the course's live list

#### Scenario: Non-owner cannot manage
- **WHEN** an Instructor who does not own the course tries to create or edit a live session
- **THEN** the system SHALL deny access

### Requirement: Live room with chat and check-in

The system SHALL provide a live room with the stream, per-session chat, and a one-time check-in for enrolled attendees.

#### Scenario: Join room
- **WHEN** an enrolled Student opens a live session
- **THEN** they see the stream (when live), the session chat, and a check-in button

#### Scenario: Check-in
- **WHEN** an enrolled Student checks in during a session
- **THEN** their attendance is recorded once

#### Scenario: Stream key secrecy
- **WHEN** a user who is not the instructor or co-host views the room
- **THEN** the stream key is never shown

### Requirement: Live sessions can be replayed

The system SHALL allow an Instructor to attach a recording to an ended session and SHALL show the replay to enrolled students.

#### Scenario: Replay
- **WHEN** a session has ended with a recording attached
- **THEN** enrolled students can watch the replay from the session
