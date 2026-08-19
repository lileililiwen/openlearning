## Why

The platform tracks lessons and quizzes but has no assignment workflow. The reference system's Practice & Exam module requires assignment distribution, submission, feedback/grading, and resubmission — a core teaching loop.

## What Changes

- Instructors create assignments on a course with instructions and an optional due date.
- Enrolled students submit an assignment (text and/or file upload).
- Instructors view submissions, grade with a score and feedback.
- Students see grades/feedback and can resubmit before grading (or after, if allowed).

## Capabilities

### New Capabilities
- `assignments`: course assignments with submission, grading, feedback, and resubmission.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Assignments` module: `Assignment { Id, CourseId, AuthorId, Title, Instructions, DueAt }`, `AssignmentSubmission { Id, AssignmentId, StudentId, Text, FileUrl, SubmittedAt, Score, Feedback, GradedAt, GradedBy }`.
- `AssignmentService` (CRUD owner-gated, submit, list, grade); `FileUrl` reuses `file-storage`.
- Pages under `Pages/Courses/Assignments/` (list, create/edit, submit, submissions, grade); dashboard/roster links.
