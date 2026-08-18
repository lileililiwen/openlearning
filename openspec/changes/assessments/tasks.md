# Assessments — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Assessments` class library and add it to the solution
- [x] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)

## 2. Data Model

- [x] 2.1 Add entities: Quiz, Question, AnswerOption, QuizAttempt, QuizAttemptAnswer
- [x] 2.2 Add `IEntityTypeConfiguration` classes (indexes, cascades, uniqueness)
- [x] 2.3 Register `ApplyConfigurationsFromAssembly` for the module in `ApplicationDbContext`
- [x] 2.4 Create `AddAssessmentsModule` DI extension and register it in `Program.cs`

## 3. Services

- [x] 3.1 Implement `QuizService`: create/update/delete/list quizzes (owner-only)
- [x] 3.2 Implement `QuestionService`: add/update/delete questions with exactly one correct option (owner-only)
- [x] 3.3 Implement `AttemptService`: submit/score attempts, list attempts by quiz and by student

## 4. UI

- [x] 4.1 Quiz list section on course edit page and course detail page (students)
- [x] 4.2 Quiz create/edit pages (owner-only)
- [x] 4.3 Question add/edit/delete pages (owner-only)
- [x] 4.4 Take-quiz page with per-question radio options (enrolled students)
- [x] 4.5 Attempt result page (score + per-question breakdown)
- [x] 4.6 Attempt results list per quiz (owner)

## 5. Migration & Verification

- [x] 5.1 Create EF Core migration for assessments tables
- [x] 5.2 Run `dotnet build` and start the app
- [x] 5.3 Verify quiz flows end-to-end (create → questions → take → score → results)
