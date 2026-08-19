# Question Types — Tasks

## 1. Data & Model

- [x] 1.1 Add `QuestionType` enum + `QuestionType` column; extend `QuestionConfiguration`
- [x] 1.2 Add `TextAnswer`, `FileAnswerUrl`, `IsGraded`, `GradedScore`, `GradingFeedback` to `QuizAttemptAnswer`
- [x] 1.3 Update `QuestionService` create/edit to render/accept each type

## 2. Taking & Scoring

- [x] 2.1 Quiz-take page renders each question type with correct inputs
- [x] 2.2 `AttemptService.SubmitAsync` validates + stores answers per type; auto-scores objective questions
- [x] 2.3 Results page shows per-question correctness and "pending grading" for manual types

## 3. Instructor Grading

- [x] 3.1 Results/grade page: list pending manual answers, grade with score + feedback
- [x] 3.2 Updating grades recalculates the attempt's score/percent

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: each type renders/takes, objective auto-score, short-answer/file-upload pending→graded→score updates
