# progress-tracking Specification

## Purpose
TBD - created by archiving change initial-lms-mvp. Update Purpose after archive.
## Requirements
### Requirement: Student can mark lessons complete

The system SHALL allow an enrolled Student to mark a lesson in the course as complete and to unmark it.

#### Scenario: Mark lesson complete
- **WHEN** an enrolled Student marks a lesson as complete
- **THEN** a completion record is stored for that Student and lesson

#### Scenario: Unmark lesson
- **WHEN** an enrolled Student unmarks a completed lesson
- **THEN** the completion record is removed

### Requirement: Course progress percentage

The system SHALL calculate a Student's progress in a course as completed lessons divided by total lessons.

#### Scenario: Progress calculation
- **WHEN** a Student has completed 3 of 6 lessons in a course
- **THEN** the course progress is shown as 50%

#### Scenario: Empty course progress
- **WHEN** a course has no lessons
- **THEN** the course progress is shown as 0%

### Requirement: Only enrolled students can track progress

The system SHALL restrict completion marking to Students enrolled in the course.

#### Scenario: Non-enrolled student cannot mark complete
- **WHEN** a Student who is not enrolled attempts to mark a lesson complete
- **THEN** the system SHALL deny the request

