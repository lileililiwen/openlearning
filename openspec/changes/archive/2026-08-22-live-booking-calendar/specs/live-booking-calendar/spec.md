## ADDED Requirements

### Requirement: Instructors configure optional session booking

The system SHALL allow the owning Instructor to enable booking, set an opening/closing window, capacity, and cancellation deadline for a live session.

#### Scenario: Booking remains optional
- **WHEN** booking is disabled for a session
- **THEN** existing enrolled-student live access behavior remains unchanged

### Requirement: Eligible learners reserve seats without overbooking

The system SHALL allow an eligible enrolled learner to hold at most one booking per session and SHALL allocate no more confirmed seats than capacity under concurrent requests.

#### Scenario: Concurrent final seat
- **WHEN** multiple eligible learners request the final available seat concurrently
- **THEN** exactly one is confirmed and the others receive deterministic waitlist positions

#### Scenario: Closed booking window
- **WHEN** a learner requests a seat outside the booking window
- **THEN** the request is rejected with the applicable window

### Requirement: Cancellation promotes the waitlist

The system SHALL permit cancellation before the deadline and SHALL atomically promote the earliest still-eligible waitlisted learner.

#### Scenario: First learner is no longer eligible
- **WHEN** a seat opens and the first waitlisted learner has lost course access
- **THEN** that entry is skipped and the next eligible learner is promoted and notified

### Requirement: Users have scoped calendars and secure feeds

The system SHALL provide authorized calendar views and revocable personal iCalendar feeds containing relevant sessions without secrets.

#### Scenario: Revoked feed token
- **WHEN** a calendar feed token is revoked
- **THEN** subsequent requests with that token are denied
