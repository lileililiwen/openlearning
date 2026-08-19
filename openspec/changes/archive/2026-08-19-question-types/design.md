# Question Types — Design

## Context

Quizzes are multiple-choice only. Adding true/false, fill-in-the-blank, short answer, and file-upload answers aligns assessments with the reference system.

## Goals

- Support the four new question types in quiz creation and taking.
- Auto-score objective types; flag manual types as pending grading.
- Let instructors grade manual answers and update scores.

## Non-Goals

- No randomized question order (deferred).
- No partial-credit UI beyond manual override.
- No essay rubric engine (manual grade with free-text feedback).

## Decisions

### D1: `QuestionType` enum on `Question`
`SingleChoice`, `MultipleChoice` (both map to existing `AnswerOption` sets), `TrueFalse` (stored as two options True/False or a boolean answer), `FillBlank` (one or more blank answers compared case-insensitively after trim), `ShortAnswer` (text, manual), `FileUpload` (text prompt, file answer, manual).

### D2: Answer shape
`QuizAttemptAnswer` gains `TextAnswer` (string, for true-false/fill-blank/short-answer) and `FileAnswerUrl` (nullable, for file upload) alongside the existing `SelectedOptionIds` (many-to-many used by multiple choice). One row per question.

### D3: Scoring
`AttemptService` computes auto score over objective questions; manual questions are excluded from the auto score and tracked via `IsGraded`. Instructor's `Results` page lists pending answers with a grade form (score + feedback) that updates the attempt total. The result page shows "pending" for ungraded items.

## Risks / Trade-offs

- **Answer normalization** → Fill-blank compares trimmed, case-insensitive, whitespace-collapsed text.
- **Score semantics** → A partially manual quiz's percent reflects only auto-graded questions until all manual items are graded; documented on the results page.

## Migration Plan

One migration adds `QuestionType`, `TextAnswer`, `FileAnswerUrl`, `IsGraded`, `GradedScore`, `GradingFeedback`.

## Open Questions

- File upload storage — reuse `file-storage` URLs (designed there).
