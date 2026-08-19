# Exams — Design

## Context

Quizzes provide practice scoring. Formal exams add time limits, attempt limits, anti-cheating signals, and a reviewable result with an incorrect-answer log.

## Goals

- Instructors define mock/official exams with duration, pass threshold, and attempt limits.
- Students take exams under a timer with anti-screen-switching detection.
- Results include an incorrect-answer log and review of correct answers.

## Non-Goals

- No live proctoring (camera/mic).
- No question bank randomization beyond the existing question order.
- No printed/signed exam certificates (certificates module is course-completion based).

## Decisions

### D1: New `OpenLearning.Exams` module
`Exam { Id, CourseId, AuthorId, Title, Description, IsOfficial, DurationMinutes, PassPercent, MaxAttempts, OpensAt?, ClosesAt? }`; `ExamAttempt { Id, ExamId, StudentId, StartedAt, SubmittedAt, Score, Percent, Passed, ScreenSwitchCount, Status }`. Exam questions reuse the assessments `Question`/`AnswerOption` model via a `Quiz`-like container (`Exam` holds `Questions` like quizzes do) — decided: exams embed a question set identical to a quiz's structure and are taken through a dedicated page.

### D2: Taking mode
`/Courses/Exams/Take` renders one question per screen with a countdown; auto-submits at 0. `visibilitychange`/`blur` increments `ScreenSwitchCount`; at the configured max (default 3) the attempt auto-submits. Submission stores answers (reusing `QuizAttemptAnswer` shapes) and computes auto score (manual question types excluded until graded — aligned with `question-types`).

### D3: Results & review
Result page shows percent/pass, screen-switch count, and per-question incorrect answers with correct answers (review mode). The incorrect-answer log is a query over the attempt's answers where `IsCorrect == false`.

### D4: Attempt limits
`MaxAttempts` (mock default 3, official 1). `StartAsync` checks attempts used within the window (`OpensAt`/`ClosesAt`) and denies if exceeded.

## Risks / Trade-offs

- **Client-side anti-switch** → Soft signal, not proof; recorded and shown in results (documented).
- **Reusing quiz question model** → Keeps `question-types` compatible and avoids duplicating scoring logic.

## Migration Plan

One migration creates `Exams` and `ExamAttempts`.

## Open Questions

- Should exam results affect course completion? MVP: no.
