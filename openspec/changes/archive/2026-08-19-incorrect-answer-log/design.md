# Incorrect Answer Log — Design

## Context

Wrong answers are scored and forgotten. A persistent log supports deliberate practice.

## Goals

- Automatically record wrong answers from quiz/exam attempts.
- Let students review, re-practice, and bookmark questions.
- Remove/resolve entries when answered correctly in practice.

## Non-Goals

- No spaced-repetition scheduling.
- No cross-user analytics.
- No editing of recorded answers (immutable history).

## Decisions

### D1: Entities in `OpenLearning.Assessments`
`IncorrectAnswer { Id, UserId, QuestionId, CourseId, ChosenAnswer, CorrectAnswer, SourceType (Quiz/Exam), SourceId, CreatedAt, ResolvedAt? }` (index on `(UserId, ResolvedAt)`). `BookmarkedQuestion { Id, UserId, QuestionId, CreatedAt }` (unique `(UserId, QuestionId)`). Kept in the Assessments module since it owns question scoring; exams reference it via `SourceType`.

### D2: Recording hook
`AttemptService`/`ExamService` compute per-question correctness after scoring; every incorrect auto-scored answer calls `IncorrectAnswerService.RecordAsync` (dedupe: one active row per `(UserId, QuestionId, SourceId)`).

### D3: Practice mode
`/Practice` lists the log (filtered unresolved/bookmarked). "Practice" builds a quiz from the logged questions (all objective types), submits through the existing attempt flow with scoring, and on correct answers marks the matching `IncorrectAnswer.ResolvedAt`.

## Risks / Trade-offs

- **Log growth** → Resolve-on-correct keeps active entries bounded; resolved history can be pruned by admin later.
- **Practice scoring** → Practice attempts are lightweight: no attempt-limit or pass requirement; they just drive resolution.

## Migration Plan

One migration creates `IncorrectAnswers` and `BookmarkedQuestions`.

## Open Questions

- Should manual-graded questions appear in the log? Only when graded incorrect — yes.
