## Why

The MVP covers course delivery and progress tracking, but there is no way to verify what a student has actually learned. Quizzes close that loop: instructors can assess understanding per course, and students get immediate scored feedback.

## What Changes

- Add a new `assessments` capability: course quizzes with multiple-choice questions.
- Instructors create and manage quizzes for courses they own. Each quiz has a title, description, and ordered questions; each question has text, a point value, and 2-4 answer options with exactly one correct answer.
- Enrolled students can take a quiz. Submission records a scored attempt (correct / total points) and shows the result immediately.
- Instructors can view attempt results for their quizzes; students can review their own attempts.
- New `OpenLearning.Assessments` class library following the modular-monolith structure (entities, EF configurations, services, `AddAssessmentsModule`), wired into the central `ApplicationDbContext` via assembly scanning.
- No breaking changes to existing capabilities.

## Capabilities

### New Capabilities
- `assessments`: Course quizzes — quiz CRUD by course owners, multiple-choice question banks, student attempts with automatic scoring, and attempt-result visibility.

### Modified Capabilities

None.

## Impact

- New `src/OpenLearning.Assessments` project referencing `OpenLearning.Auth` (student/instructor identity), `OpenLearning.CourseManagement` (Course ownership), and `OpenLearning.Enrollment` (enrolled-only gating).
- New tables: `Quizzes`, `Questions`, `AnswerOptions`, `QuizAttempts`, `QuizAttemptAnswers`; one EF Core migration.
- New Razor Pages under `Pages/Courses/Quizzes/` (manage, edit, question CRUD, take, results) plus links from the course edit page and course detail page.
- No changes to existing modules' behavior or specs.
