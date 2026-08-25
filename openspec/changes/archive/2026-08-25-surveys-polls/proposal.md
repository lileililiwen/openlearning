## Why

Surveys and polls are table stakes in every LMS we surveyed (Moodle Feedback/Choice, Canvas surveys) and serve course feedback, live-session polling, and quick checks — but OpenLearning has zero survey capability today. Ratings-reviews covers course reviews only and Q&A covers questions, neither replaces structured instructor-authored questionnaires.

## What Changes

- Add surveys with single-choice, multiple-choice, rating-scale, and open-text questions, scoped to a course or the whole platform.
- Add response windows (open/close times) and one-response-per-user enforcement.
- Add anonymous mode where responses are stored without respondent identity and results show only aggregates.
- Add aggregate result views for the author, gated by policy (after close by default).
- Keep surveys non-graded: no effect on grades, progress, credits, or certificates.

## Capabilities

### New Capabilities
- `surveys-polls`: survey definition, question types, response collection, anonymity, windows, and aggregate results.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Survey` domain module; course-scope authorization reuses enrollment and ownership checks.
- New Razor Pages under Courses and Admin feature areas.
- New EF Core migration; optional notification on survey open via existing notification events.
