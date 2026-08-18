# student-dashboard Specification

## Purpose
TBD - created by archiving change dashboards. Update Purpose after archive.
## Requirements
### Requirement: Student has a personalized dashboard

The system SHALL provide a dashboard for authenticated Students that summarizes their learning state and deep-links into their courses.

#### Scenario: Student signs in and sees dashboard
- **WHEN** a Student signs in and is redirected to their dashboard
- **THEN** the dashboard shows their enrolled courses with progress, quiz status, and any certificates earned

#### Scenario: Continue learning
- **WHEN** a Student has previously opened lessons in an enrolled course
- **THEN** the dashboard offers a "Continue learning" action that opens the most recently accessed unfinished lesson

#### Scenario: Recommendations
- **WHEN** a Student views their dashboard
- **THEN** the dashboard suggests published courses from the same categories as their enrolled courses

### Requirement: Lesson access is tracked for resume

The system SHALL record when a Student opens a lesson in an enrolled course so the dashboard can resume at the right lesson.

#### Scenario: Open lesson records access
- **WHEN** an enrolled Student opens a lesson
- **THEN** the lesson is recorded as the most recently accessed lesson for that enrollment

