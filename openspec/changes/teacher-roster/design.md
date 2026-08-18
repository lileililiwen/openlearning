# Teacher Roster & Student Progress — Design

## Context

Enrollment, progress, quiz, and SCORM data all exist and are keyed by enrollment, but nothing surfaces them to the instructor. This change composes those existing services into a roster + per-student progress view.

## Goals

- Course owner can see who is enrolled.
- Course owner can see an individual student's progress (lessons, quizzes, SCORM, last activity).
- Course owner can withdraw a student.

## Non-Goals

- No per-student messaging (covered by chat/notifications).
- No bulk actions or CSV export here (see `platform-analytics`).
- No changes to what students can see.

## Decisions

### D1: Roster over existing data
`EnrollmentService` gains `GetRosterAsync(courseId, ownerId)`: enrollments (with student) plus per-student completion percentage and last activity. A single query per course (projection) avoids N+1.

### D2: Per-student progress detail
A `StudentProgress` view model composes:
- Lesson completions (from `ProgressService`).
- Quiz attempts + scores (from `AttemptService`).
- SCORM records (from `ScormRuntimeService`).
- Last lesson access (from the `LessonAccess` marker added in `dashboards`).
Ownership is enforced by verifying the caller is the course owner before composing.

### D3: Withdraw action
Reuses `EnrollmentService.WithdrawAsync` behind a course-owner check, with a confirmation page/flow. Withdrawing deletes the enrollment (and cascades completions/SCORM records), matching current semantics.

## Risks / Trade-offs

- **Cross-module composition** → The page (Web) orchestrates module services; modules stay acyclic.
- **Student privacy** → Roster visible only to the course owner; admin can view via `user-management` detail later.

## Migration Plan

No schema changes.

## Open Questions

- Should the roster be filterable by progress/activity? Nice-to-have; MVP lists all with search by name.
