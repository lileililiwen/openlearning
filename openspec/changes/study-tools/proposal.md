## Why

The reference system's Study Tools module — study notes, study plans (check-ins, calendar, reports), and downloads — is absent. These features deepen engagement and give students a sense of progress beyond completion percent.

## What Changes

- Lesson study notes: per-lesson notes editable and exportable (Markdown/plain text download).
- Study plan: daily check-ins, a study calendar, and a study report (duration, streaks, completed lessons).
- Downloads: permitted files (courseware PDFs, and video when the course allows it) downloadable per lesson.

## Capabilities

### New Capabilities
- `study-tools`: lesson notes with export, study plan/check-ins/calendar, and lesson downloads.

### Modified Capabilities

- `progress-tracking`: study duration tracking feeds the study report (see `study-duration`).

## Impact

- New `OpenLearning.StudyTools` module: `LessonNote { Id, UserId, LessonId, Body, UpdatedAt }` (unique `(UserId, LessonId)`), `StudyCheckIn { Id, UserId, Day, Note? }` (unique `(UserId, Day)`), `LessonDownload { Id, LessonId, FileUrl, Label, IsAllowed }`.
- `StudyToolService` (notes CRUD, export, check-in, calendar/report queries, downloads list).
- Pages under `/Study` (notes, plan/check-in, report) plus lesson-page note/export and download buttons.
