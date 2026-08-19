# study-duration Specification

## Purpose
TBD - created by archiving change study-duration. Update Purpose after archive.
## Requirements
### Requirement: Study duration is tracked

The system SHALL track the time a Student spends studying each lesson and SHALL expose daily, per-lesson, per-course, and per-student totals.

#### Scenario: Session duration
- **WHEN** an enrolled Student opens a lesson and studies for a period
- **THEN** the study time is accumulated for that lesson and day

#### Scenario: Idle time excluded
- **WHEN** the lesson tab is hidden or idle beyond the heartbeat interval
- **THEN** the idle time is not counted

#### Scenario: Daily totals
- **WHEN** a Student views their study report
- **THEN** study duration per day is shown

#### Scenario: Instructor sees student duration
- **WHEN** an Instructor views a course's roster or a student's progress
- **THEN** the student's total study duration for the course is shown

### Requirement: Study duration is capped

The system SHALL limit counted study time per user per day to prevent abuse.

#### Scenario: Abuse cap
- **WHEN** a user exceeds the daily duration cap
- **THEN** additional time is not counted

