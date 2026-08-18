## Why

The assessment module only supports multiple-choice questions. The reference system's Practice & Exam module lists single-choice, multiple-choice, true/false, fill-in-the-blank, short answer, and file upload answers. Broader question types are required for realistic quizzes, assignments, and exams.

## What Changes

- Question types: single-choice (existing), multiple-choice (existing), plus true/false, fill-in-the-blank, short answer, and file-upload answer.
- Auto-scoring for objective types (single/multiple/true-false/fill-blank); short answer and file upload are graded manually by the instructor.
- Quiz results show per-question correctness; manual-graded questions show a "pending grading" state.

## Capabilities

### New Capabilities
- `question-types`: expanded question types and mixed auto/manual grading.

### Modified Capabilities

- `assessments`: question model supports new kinds and answer shapes; scoring handles each type; attempts store textual/file answers.

## Impact

- `Question` gains `QuestionType` (enum: SingleChoice, MultipleChoice, TrueFalse, FillBlank, ShortAnswer, FileUpload); `QuestionConfiguration` extends max lengths.
- `QuizAttemptAnswer` gains `TextAnswer` and `FileAnswerUrl` in addition to `SelectedOptionIds`.
- `AttemptService.SubmitAsync` validates per type; `ScorePercent` only counts auto-scored questions until manual grading (a `Graded` flag on short-answer/file answers).
