# enrollment Specification

## Purpose
TBD - created by archiving change initial-lms-mvp. Update Purpose after archive.
## Requirements
### Requirement: Student can enroll in a published course

The system SHALL allow an authenticated Student to enroll in any Published course. The course is then added to their "My Courses" list.

#### Scenario: Student enrolls in course
- **WHEN** a Student clicks Enroll on a published course
- **THEN** an enrollment record is created for that Student and course
- **THEN** the course appears in the Student's enrolled courses

### Requirement: Duplicate enrollment is prevented

The system SHALL prevent a Student from enrolling in the same course more than once.

#### Scenario: Second enrollment attempt
- **WHEN** a Student attempts to enroll in a course they are already enrolled in
- **THEN** the system SHALL reject the request and keep a single enrollment

### Requirement: Draft courses cannot be enrolled

The system SHALL NOT allow enrollment in Draft courses.

#### Scenario: Enroll on draft course
- **WHEN** a Student attempts to enroll in a Draft course
- **THEN** the system SHALL reject the request

### Requirement: Student can withdraw from a course

The system SHALL allow a Student to withdraw from an enrolled course, removing the enrollment.

#### Scenario: Student withdraws
- **WHEN** a Student withdraws from an enrolled course
- **THEN** the enrollment is removed
- **THEN** the course disappears from the Student's enrolled list

