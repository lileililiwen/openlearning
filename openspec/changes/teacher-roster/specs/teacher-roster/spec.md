## ADDED Requirements

### Requirement: Teacher can view enrolled students

The system SHALL allow the course owner to view the list of students enrolled in their course, with each student's completion percentage and last activity.

#### Scenario: View roster
- **WHEN** the owning Instructor opens a course's roster
- **THEN** enrolled students are listed with their progress and last activity

#### Scenario: Non-owner is denied
- **WHEN** an Instructor who does not own the course attempts to view the roster
- **THEN** the system SHALL deny access

### Requirement: Teacher can view per-student progress

The system SHALL allow the course owner to view a single student's progress in the course: completed lessons, quiz attempts and scores, SCORM state, and last accessed lesson.

#### Scenario: Open student progress
- **WHEN** the owning Instructor opens a student's progress detail
- **THEN** completed lessons, quiz scores, SCORM status, and last access are shown

### Requirement: Teacher can withdraw a student

The system SHALL allow the course owner to remove a student from a course, deleting the enrollment and its associated records.

#### Scenario: Withdraw student
- **WHEN** the owning Instructor confirms withdrawing a student
- **THEN** the enrollment (and its completions/records) is removed and the student loses access
