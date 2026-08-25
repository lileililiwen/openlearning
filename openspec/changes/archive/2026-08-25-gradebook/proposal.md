## Why

Grades exist per feature — assignment scores, quiz attempts, exam results — but no unified per-course gradebook aggregates them into a weighted course grade. Every academic LMS we surveyed (Canvas, Moodle, Sakai) treats the gradebook as the instructor's operational center of gravity; our research flagged its absence as an academic-segment table-stakes gap.

## What Changes

- Add a per-course gradebook where the owning Instructor selects graded items (assignments, quizzes, exams) and assigns weights that must total 100%.
- Compute each student's running aggregate from graded scores only, server-side and deterministically.
- Support per-student item overrides and excusals (excused items are excluded from the weight denominator).
- Give students a read-only view of their item scores and aggregate once the Instructor publishes the gradebook.
- Keep all source-of-record scores in their owning modules; the gradebook stores configuration, overrides, and computed snapshots only.

## Capabilities

### New Capabilities
- `gradebook`: weighted item configuration, aggregate computation, overrides/excusals, publication control, and student visibility.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Gradebook` domain module reading graded outcomes from assignments, assessments, and exams via their services.
- New Razor Pages under each course's teaching area for configuration, grid management, and student view.
- New EF Core migration; complements (does not duplicate) `grade-export`, which can later source rows from the gradebook.
