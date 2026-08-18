## Why

Progress is binary (lesson complete or not) and resume points exist, but there is no measure of how long a student actually studied. The reference system's Lesson Study module lists "study duration statistics", which feed dashboards and the study report.

## What Changes

- Record study sessions: opening a lesson starts a session; closing/leaving ends it; duration accumulates per (user, course, lesson, day).
- Per-lesson and per-day duration statistics for students (own) and instructors (per student in a course).
- Feeds the `study-tools` report and the teacher roster/student view.

## Capabilities

### New Capabilities
- `study-duration`: accumulated study time per lesson and per day.

### Modified Capabilities

- `progress-tracking`: lesson access records sessions and duration.
- `teacher-roster`: per-student study duration shown.

## Impact

- New `StudySession { Id, UserId, CourseId, LessonId, EnrollmentId, StartedAt, EndedAt, DurationSeconds }` in the Progress module.
- `ProgressService` gains `StartSessionAsync`, `EndSessionAsync`, `GetStudyDurationAsync` (per lesson/user, per day), `GetDailyDurationsAsync`.
- Lesson `View` starts/ends sessions (JS `visibilitychange`/unload + a heartbeat POST); dashboard/roster show duration.
