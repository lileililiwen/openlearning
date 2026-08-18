## Why

A teacher can manage content and see quiz results, but cannot see who is enrolled in their course or how each student is progressing. The core teaching loop — knowing your students — is missing.

## What Changes

- Teachers can view the **roster** of enrolled students for a course they own.
- Teachers can open a **per-student progress view**: lessons completed, quiz attempts and scores, SCORM status, last activity.
- Teachers can **withdraw** a student from a course (with confirmation).
- The roster is a read surface over existing enrollment/progress/assessment/SCORM data; small query helpers only.

## Capabilities

### New Capabilities
- `teacher-roster`: Enrolled-student roster, per-student progress monitoring, and student withdrawal for course owners.

### Modified Capabilities

None.

## Impact

- New Razor Pages under `Pages/Courses/Roster/` (list + per-student detail), linked from the course edit page and teacher dashboard.
- Aggregation/query helpers added to `EnrollmentService` (roster), `ProgressService` (per-student completion set), `AttemptService` (per-student quiz attempts), and `ScormRuntimeService` (per-student SCORM records).
- No schema changes.
