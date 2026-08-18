# Lesson Preview — Design

## Context

Course content is fully gated by enrollment. Preview lessons are the standard way to let prospects sample a course before enrolling.

## Goals

- Instructors mark specific lessons as previews.
- Non-enrolled visitors can open preview lessons of published courses.
- Enrolled students see preview lessons like normal lessons (no change).

## Non-Goals

- No per-lesson time-limited trials.
- No watermarking for previews (video-player change handles media-level controls).
- No auto-preview of the first lesson (explicit flag only).

## Decisions

### D1: `Lesson.IsPreview` flag
`Lesson` gains `bool IsPreview` (default false). Lesson create/edit forms add a checkbox. Course details renders preview lessons with a badge and links them for everyone.

### D2: Access rule on `View`
In `Pages/Courses/Lessons/View`: if the course is published and `lesson.IsPreview`, allow access even when not enrolled/owner/admin. Draft courses still forbid non-owners. When a non-enrolled user views a preview, skip `RecordAccessAsync`/`MarkCompleteAsync` (no progress for non-enrolled).

### D3: Visibility on details
`Pages/Courses/Details` shows all lessons when enrolled; otherwise it lists only preview lessons (title + link) plus a "content available after enroll" note for the rest.

## Risks / Trade-offs

- **Content leakage** → Only lessons explicitly flagged as preview are exposed; default remains gated.
- **Progress edge case** → Guard all progress calls behind the enrollment check, not just the view check.

## Migration Plan

One migration adds `IsPreview` to `Lessons`.

## Open Questions

- Should quizzes also be previewable? Deferred — lessons only.
