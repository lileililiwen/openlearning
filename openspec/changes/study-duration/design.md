# Study Duration — Design

## Context

Completion percent and resume position exist, but study time is not tracked. Duration is a key engagement metric and feeds the study report.

## Goals

- Track study sessions per lesson with a duration.
- Show per-day and per-lesson totals to the student.
- Show per-student totals to the instructor (roster/student view).

## Non-Goals

- No "active window" heuristic beyond a simple heartbeat (a session is the time between open and close, minus idle periods where the heartbeat stops).
- No platform-wide analytics (that's `platform-analytics`).

## Decisions

### D1: `StudySession` in the Progress module
`StudySession { Id, UserId, CourseId, LessonId, EnrollmentId?, StartedAt, EndedAt?, DurationSeconds }`. Recording lives in `ProgressService` (already the lesson-access owner).

### D2: Session lifecycle (client-driven)
Lesson `View` starts a session via POST `/progress/session/start` on load, then a heartbeat POST every 60s while the tab is visible, and an end POST on `visibilitychange`→hidden/`pagehide`. Server accumulates `DurationSeconds` per heartbeat (idle gaps > 2× heartbeat are not counted). Sessions are capped (e.g. 4h/day) to prevent abuse.

### D3: Queries
`ProgressService`: `GetLessonDurationAsync(userId, lessonId)`, `GetDailyDurationsAsync(userId, from, to)` (day → total seconds), `GetCourseDurationAsync(userId, courseId)`, and `GetStudentDurationsAsync(courseId)` (enrollment → total) for the roster/student view.

## Risks / Trade-offs

- **Idle inflation** → Heartbeat gap rule limits counted time; documented.
- **Multi-tab** → One active session per (user, lesson) — starting a new one ends the previous.

## Migration Plan

One migration creates `StudySessions`.

## Open Questions

- Should SCORM/video duration also count? Yes — both reuse the same session flow on the lesson page.
