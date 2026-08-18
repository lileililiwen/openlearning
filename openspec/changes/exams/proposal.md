## Why

Quizzes are lightweight and repeatable, but the reference system distinguishes formal exams: mock and official exams with a timer, anti-screen-switching, a result record, an incorrect-answer log, and exam review. Formal exams support assessment for credentials.

## What Changes

- Instructors create exams (mock or official) with a time limit, pass threshold, and optional scheduling window.
- A dedicated exam-taking mode with a countdown timer and anti-screen-switching detection.
- On submit (or timeout) an exam result is recorded; the student sees results and an incorrect-answer log with the correct answers for review.
- Exam attempts are limited (e.g. mock: 3, official: 1) unless the instructor allows retakes.

## Capabilities

### New Capabilities
- `exams`: formal exams with timer, anti-switch, results, incorrect-answer log, and review.

### Modified Capabilities

- `assessments`: reuses the expanded `question-types`; exam results reference attempts.

## Impact

- New `OpenLearning.Exams` module: `Exam { Id, CourseId, AuthorId, Title, Description, IsOfficial, DurationMinutes, PassPercent, MaxAttempts, OpensAt?, ClosesAt? }`, `ExamAttempt { Id, ExamId, StudentId, StartedAt, SubmittedAt, Score, Percent, Passed, ScreenSwitchCount, Status }`.
- `ExamService` (CRUD owner-gated, start/attempt, submit, results, incorrect log).
- Pages under `Pages/Courses/Exams/` (list, create/edit, take with timer, result/review).
- Anti-switch: `visibilitychange`/`blur` events increment `ScreenSwitchCount`; exceeding a limit auto-submits.
