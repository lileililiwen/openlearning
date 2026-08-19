# Exams — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Exams` class library, add to solution, add references (Auth, CourseManagement, Enrollment, Assessments, EF Core)
- [x] 1.2 Add `Exam` + `ExamAttempt` entities + configs
- [x] 1.3 Implement `ExamService` (CRUD owner-gated, start, submit, results, attempt limits)
- [x] 1.4 Register assembly scanning + `AddExamsModule`

## 2. Taking & Results UI

- [x] 2.1 Exam list on course + create/edit for owner (mock/official, duration, pass %, max attempts, window)
- [x] 2.2 Take page: countdown timer, auto-submit at 0, anti-screen-switch counter + auto-submit
- [x] 2.3 Result page: percent/pass, screen switches, incorrect-answer log with correct answers (review)

## 3. Attempt Limits

- [x] 3.1 Enforce MaxAttempts and OpensAt/ClosesAt in `StartAsync`

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: take/submit/timeout, pass/fail, incorrect log, attempt limit, non-owner denied
