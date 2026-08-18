# Incorrect Answer Log — Tasks

## 1. Data & Service

- [ ] 1.1 Add `IncorrectAnswer` + `BookmarkedQuestion` entities + configs in the Assessments module
- [ ] 1.2 Implement `IncorrectAnswerService` (record, list, resolve, bookmark toggle, practice build)
- [ ] 1.3 Hook `AttemptService` (and `ExamService`) scoring to record incorrect answers

## 2. UI

- [ ] 2.1 `/Practice` page: log list (unresolved/bookmarked filters) + bookmark toggle
- [ ] 2.2 Practice quiz page built from logged questions; correct answers resolve entries

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: wrong answers logged after quiz/exam, practice correct resolves, bookmark persists, log filters
