# Gradebook — Tasks

## 1. Domain

- [x] 1.1 Add the Gradebook project with configuration, item, override/excusal, and publication snapshot models plus configurations
- [x] 1.2 Implement item resolution through assignments/assessments/exams services and weight-normalized aggregate computation
- [x] 1.3 Implement overrides, excusals with audit fields, publication snapshots, and student-scoped read projections
- [x] 1.4 Add database registration and an EF Core migration

## 2. Workflows

- [x] 2.1 Add Instructor pages: gradebook builder (items + weights), student grid with overrides/excusals, publish action
- [x] 2.2 Add student course-grades page gated by publication showing own rows only

## 3. Verification

- [x] 3.1 Test weight-total rejection, partial-grading aggregates, excusal denominator math, override precedence, unpublished hiding, peer-row isolation, and non-owner denial
- [x] 3.2 Build cleanly and exercise every scenario over HTTP
