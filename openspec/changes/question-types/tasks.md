# Question Types — Tasks

## 1. Data & Model

- [ ] 1.1 Add `QuestionType` enum + `QuestionType` column; extend `QuestionConfiguration`
- [ ] 1.2 Add `TextAnswer`, `FileAnswerUrl`, `IsGraded`, `GradedScore`, `GradingFeedback` to `QuizAttemptAnswer`
- [ ] 1.3 Update `QuestionService` create/edit to render/accept each type

## 2. Taking & Scoring

- [ ] 2.1 Quiz-take page renders each question type with correct inputs
- [ ] 2.2 `AttemptService.SubmitAsync` validates + stores answers per type; auto-scores objective questions
- [ ] 2.3 Results page shows per-question correctness and "pending grading" for manual types

## 3. Instructor Grading

- [ ] 3.1 Results/grade page: list pending manual answers, grade with score + feedback
- [ ] 3.2 Updating grades recalculates the attempt's score/percent

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: each type renders/takes, objective auto-score, short-answer/file-upload pending→graded→score updates
