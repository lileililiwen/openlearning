## ADDED Requirements

### Requirement: Student can take and export lesson notes

The system SHALL allow an enrolled Student to save notes per lesson and export them as a downloadable Markdown file.

#### Scenario: Save note
- **WHEN** an enrolled Student saves a note on a lesson
- **THEN** the note is stored for that student and lesson and shown when reopened

#### Scenario: Export note
- **WHEN** a Student exports their lesson note
- **THEN** a Markdown file containing the note is downloaded

### Requirement: Student has a study plan with check-ins

The system SHALL allow a Student to check in once per day and SHALL show a study calendar and a study report.

#### Scenario: Daily check-in
- **WHEN** a Student checks in on a day
- **THEN** that day is recorded; a second check-in the same day updates rather than duplicates

#### Scenario: Calendar and report
- **WHEN** a Student opens the study page
- **THEN** a calendar of check-ins and study durations plus a report (streak, total duration, completed lessons) are shown

### Requirement: Lesson downloads

The system SHALL allow a Student to download files the course owner has marked downloadable for a lesson, gated by enrollment.

#### Scenario: Download file
- **WHEN** an enrolled Student opens a lesson with an allowed download
- **THEN** the file download link is shown and the file can be downloaded

#### Scenario: Not allowed or not enrolled
- **WHEN** a file is not marked downloadable, or the user is not enrolled
- **THEN** the download is not offered
