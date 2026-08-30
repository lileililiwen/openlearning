# Implementation Tasks

## 1. Outcome mapping

- [x] Add outcome, activity-mapping, mastery-result, and intervention entities/configurations without introducing cross-module navigation collections.
- [x] Add migration and indexes for course, learner, outcome, and calculation version (`AddOutcomes`).

## 2. Calculation and operations

- [x] Implement deterministic mastery calculation with auditable source references and idempotent recalculation (`OutcomeOperationsService.CalculateWithAsync` / `RecalculateAsync`).
- [x] Implement explainable intervention rules (MissingWork, RepeatedLowAttempts, StaleProgress, UnmetOutcome) and role/tenant scoping.
- [x] Add unit tests for thresholds, boundary, missing work, corrections, and duplicate recalculation (9 service tests pass).

## 3. UI and integration

- [x] Add instructor outcome/learner operations view (`Pages/Courses/Outcomes/Index`) with outcome + mapping management and a mastery/signal matrix, owner + Admin gated, linked from the course Edit page.
- [x] Integrate existing assessment results through an explicit service interface (`IOutcomeActivitySource`); Web provides `DbOutcomeActivitySource` reading `QuizAttempt` scores. Gradebook/analytics can be added via the same interface.
- [x] Add authorization and PostgreSQL integration tests, plus role-scoped Web page tests (owner / non-owner / student).

## 4. Verification

- [x] Run architecture tests (5/5), Outcomes unit + PostgreSQL + Web page tests (19 pass), and `dotnet build OpenLearning.sln` (0 warnings / 0 errors).
- [x] EF migration `AddOutcomes` generated (tables `CourseOutcome`, `OutcomeActivityMapping`, `MasteryResult` + indexes).
