# Gradebook — Design

## Context

Assignment scores, quiz attempts, and exam results each live in their owning module. Instructors currently export separate files (grade-export) but cannot see or publish one weighted course grade. Canvas/Moodle/Sakai all center instructor workflows on a gradebook; the aggregation must not corrupt source-of-record scores.

## Goals

- Weighted per-course configuration with a 100% validity rule.
- Deterministic server-side aggregates over graded items only.
- Explicit overrides/excusals with audit fields.
- Publication gate controlling all student visibility.

## Non-Goals

- Late-policy automation (candidate follow-up change).
- Multi-grader moderation workflows.
- Letter-grade scales, curving, or category drop-lowest rules (extensible later).
- Replacing grade-export; gradebook becomes an additional source for it.

## Decisions

### D0: New module reading scores through owning services

`OpenLearning.Gradebook` follows the §2 pattern. Item score resolution calls assignments/assessments/exams services by item reference — no cross-module table joins, no writes to those modules. The gradebook stores item references (type + id), weights, overrides, excusals, publication state, and computed snapshots.

### D1: Compute on read from stored inputs

Aggregates are calculated at request time from current module scores plus stored overrides — matching competency gap-analysis and analytics precedents. A snapshot is persisted only at publication so released values remain explainable while unpublished edits stay fluid.

### D2: Excusal removes weight; override replaces score

Excused items drop out of numerator and denominator (Canvas semantics). Overrides are gradebook-local rows that shadow the source score in aggregate math only. Both record actor and timestamp.

### D3: Publication is a course-level flag with student-scoped reads

One publish toggle per course gradebook. Student queries project strictly to the requesting user's enrollment row; grid pages are instructor-only behind existing ownership checks.

## Risks / Trade-offs

- Read-time computation over many items × students may need paging on large courses; acceptable at current scale.
- Source scores can change after publication (resubmissions); published snapshots preserve what students saw, with drift visible to instructors.

## Migration Plan

Add gradebook config, item, override/excusal, and publication snapshot tables. No backfill; gradebooks start empty until an Instructor configures one.
