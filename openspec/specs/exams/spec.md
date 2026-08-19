# exams Specification

## Purpose
TBD - created by archiving change exams. Update Purpose after archive.
## Requirements
### Requirement: Instructor can create exams

The system SHALL allow the course owner to create mock and official exams with a title, duration, pass threshold, attempt limit, and optional availability window.

#### Scenario: Create exam
- **WHEN** the owning Instructor creates an exam
- **THEN** enrolled students can take it within its window, subject to the attempt limit

#### Scenario: Non-owner cannot manage
- **WHEN** an Instructor who does not own the course tries to create or edit an exam
- **THEN** the system SHALL deny access

### Requirement: Student takes a timed exam

The system SHALL run an exam with a countdown timer and detect screen switching, auto-submitting on timeout or when the switch limit is exceeded.

#### Scenario: Timed submission
- **WHEN** the exam timer reaches zero
- **THEN** the attempt is auto-submitted with the answers recorded so far

#### Scenario: Anti-switch
- **WHEN** a Student leaves the exam page beyond the configured limit
- **THEN** the attempt is auto-submitted and the switch count is recorded

#### Scenario: Attempt limit
- **WHEN** a Student has used all allowed attempts
- **THEN** further attempts are denied

### Requirement: Exam results include review

The system SHALL record exam results and SHALL show the Student a score, pass/fail status, and an incorrect-answer log with correct answers.

#### Scenario: Result with review
- **WHEN** a Student completes or submits an exam
- **THEN** the result page shows the percent, pass status, and every incorrect answer with the correct answer

