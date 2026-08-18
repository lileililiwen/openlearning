# Study Tools — Design

## Context

Students can track completion but have no notes, study planning, or downloads. This change adds the personal study toolkit from the reference system.

## Goals

- Per-lesson notes, editable and exportable.
- Daily check-ins and a study calendar/report.
- Permitted file downloads per lesson.

## Non-Goals

- No gamified streaks UI beyond the report (MVP).
- No offline sync of video (download only where the instructor allows).
- No note sharing.

## Decisions

### D1: New `OpenLearning.StudyTools` module
`LessonNote { Id, UserId, LessonId, Body, UpdatedAt }` unique `(UserId, LessonId)`; `StudyCheckIn { Id, UserId, Day, Note? }` unique `(UserId, Day)`; `LessonDownload { Id, LessonId, FileUrl, Label, IsAllowed }` (instructor-configured; defaults from `file-storage`). `StudyToolService`: upsert note, get note, export note, check-in (upsert), calendar (check-ins + study duration per day), report (total duration, check-in count, streak, completed lessons).

### D2: Study duration source
The report reads per-day study durations computed by the `study-duration` change (video playback seconds + lesson open sessions). `StudyToolService` accepts a `GetDurationPerDayAsync` delegate or queries the `LessonAccess`/session data directly to stay decoupled.

### D3: UI
Lesson `View` renders a notes panel (save/export as `.md`), a "Study plan" page (`/Study`) with a check-in button, a month calendar, and a report; lesson pages list permitted downloads (files served via `file-storage` URLs, gated by enrollment).

## Risks / Trade-offs

- **Export format** → Plain text/Markdown download is simple and dependency-free.
- **Check-in integrity** → Server-side date = UTC day; one check-in per day enforced by unique index (upsert).

## Migration Plan

One migration creates `LessonNotes`, `StudyCheckIns`, `LessonDownloads`.

## Open Questions

- Should downloads require the course owner to allow them per file? Yes (`IsAllowed`), default off.
