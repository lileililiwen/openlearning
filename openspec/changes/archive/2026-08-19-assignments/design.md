# Assignments — Design

## Context

Course work is assessed via quizzes only. Assignments add open-ended work with manual grading.

## Goals

- Instructors publish assignments with instructions and due dates.
- Enrolled students submit text and/or a file.
- Instructors grade and give feedback; students see results and resubmit.
- Completion of an assignment is tracked (feeds course progress).

## Non-Goals

- No plagiarism detection.
- No peer review.
- No group assignments.

## Decisions

### D1: New `OpenLearning.Assignments` module
`Assignment { Id, CourseId, AuthorId, Title, Instructions, DueAt? }`, `AssignmentSubmission { Id, AssignmentId, StudentId, Text?, FileUrl?, SubmittedAt, Score?, Feedback?, GradedAt?, GradedBy? }`. Unique index on `(AssignmentId, StudentId)` so one submission per student; resubmission replaces the row (resets grading). `AssignmentService`: create/update/delete (owner-only), submit, get-for-student, list, grade.

### D2: Submission lifecycle
Submit → stores text/file. If the assignment is not yet graded, a second submit overwrites. If graded, resubmission is allowed only when the instructor has enabled "allow resubmit" (else a new submission after grading is rejected with a message). Grading sets `Score`, `Feedback`, `GradedAt`, `GradedBy`.

### D3: UI layout
`/Courses/Assignments` (list for course, gated by enrollment/ownership), `/Assignments/Create|Edit`, `/Assignments/Submit`, `/Assignments/Submissions` (instructor view with per-student grade form). Student dashboard shows "assignments due" count.

## Risks / Trade-offs

- **File upload** → Size/type limits enforced server-side; URLs come from `file-storage`.
- **Resubmit policy** → Default: resubmit until graded; explicit flag allows resubmit after grading. Simple and predictable.

## Migration Plan

One migration creates `Assignments` and `AssignmentSubmissions`.

## Open Questions

- Should assignments count toward course completion percent? MVP: no (lessons/quizzes only); tracked separately.
