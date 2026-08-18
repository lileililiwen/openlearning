# Assessments — Design

## Context

The LMS MVP (archived as `2026-08-18-initial-lms-mvp`) delivers courses, enrollment, and progress tracking but no learning verification. This change adds course quizzes with automatic scoring, following the established modular-monolith structure: one class library per business domain, a central `ApplicationDbContext` that discovers per-module `IEntityTypeConfiguration` classes, and a single Razor Pages UI shell.

## Goals

- Instructors (course owners) can create quizzes with ordered multiple-choice questions.
- Enrolled students can take a quiz and immediately see a score.
- Attempts are persisted; instructors and students can review results.
- New domain is fully contained in `OpenLearning.Assessments`; no edits to existing modules.

## Non-Goals

- No question types beyond single-answer multiple choice (no true/false variant, no open text, no multi-select) for the MVP.
- No quiz scheduling/timers, shuffling, or randomized question banks.
- Quizzes do not change the existing progress-tracking percentage (lessons only).
- No public leaderboards or analytics.

## Decisions

### D1: New `OpenLearning.Assessments` module
Entities, EF configurations, services, and the `AddAssessmentsModule` DI extension live in a new class library. References: `OpenLearning.Auth` (roles/identity), `OpenLearning.CourseManagement` (Course for ownership), `OpenLearning.Enrollment` (enrolled-only gating). It does **not** reference `OpenLearning.Data`; services inject the base `DbContext` and use `Set<T>()`, matching the existing pattern that avoids `Module → Data → Module` cycles.

### D2: Domain model
- `Quiz { Id, CourseId, Title, Description, OrderIndex }`
- `Question { Id, QuizId, Text, OrderIndex, Points }`
- `AnswerOption { Id, QuestionId, Text, IsCorrect, OrderIndex }` (exactly one `IsCorrect` per question, enforced by the service on save)
- `QuizAttempt { Id, QuizId, StudentId, CompletedAt, Score, MaxScore }` (unique per (Quiz, Student) is NOT enforced — multiple attempts allowed; each attempt is a fresh submission)
- `QuizAttemptAnswer { Id, AttemptId, QuestionId, AnswerOptionId, IsCorrect }`

Deleting a quiz cascades to questions, options, attempts, and attempt answers.

### D3: Scoring
`Score = sum(Points)` of questions answered correctly; `MaxScore = sum(Points)` of all questions. Computed server-side at submission. Result page shows score/max and a per-question correct/incorrect breakdown. Rationale: avoids trusting client-side scoring; keeps it stateless (no partial-credit).

### D4: Single-answer multiple choice only
Each question has 2-4 options, one correct. Rendering is a radio group per question, which keeps the take-quiz form simple and the validation logic obvious. Alternative considered: multi-select or free-text — deferred (Non-Goals).

### D5: Access control
- Quiz/question CRUD: course owner only (same check pattern as modules/lessons).
- Taking a quiz: enrolled students only; owner/instructor may preview.
- Results: the quiz owner sees all attempts; the attempt's student sees only their own.

### D6: UI placement
Quiz management lives under `Pages/Courses/Quizzes/` and is linked from the course edit page (owner) and the course detail page (students see "Quizzes" list). Take-quiz and result pages follow the existing lesson-view pattern.

## Risks / Trade-offs

- **One-correct-answer model is limiting** → Question types are an isolated enum/flag away; the service and tables already generalize (per-answer `IsCorrect`, points).
- **Quiz form grows with question count** → Single page with a radio group per question is acceptable at MVP scale; pagination deferred.
- **No progress integration** → Quizzes stay out of the lesson-completion percentage; a future change can define how quizzes affect progress.

## Migration Plan

Add one EF migration (`AddAssessments`) in `OpenLearning.Data`. `db.Database.Migrate()` on startup applies it automatically. Rollback: remove the migration and drop the new tables.

## Open Questions

- Should a passing threshold mark the course as "complete"? Deferred — the current progress model is lesson-based.
- Should students be allowed unlimited re-attempts? MVP: yes, every submission creates a new attempt; a future change may cap attempts or require a passing score.
