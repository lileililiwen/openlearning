# Design: Outcomes and Mastery Operations

## Decisions

- Outcomes belong to a course and are mapped to assessment items or activities with weights and an explicit mastery threshold.
- A mastery result stores source attempts, calculation version, timestamp, score, and state: NotStarted, Developing, Mastered, or NeedsReview.
- Recalculation is deterministic and idempotent; corrections create a new result snapshot rather than rewriting audit history.
- Intervention indicators are explainable rules based on missing work, repeated low attempts, stale progress, or unmet outcomes. They recommend review and never change grades automatically.
- Students see their own results; instructors see only learners in owned courses; admins see tenant-scoped operational data.

## Verification

Test boundary scores, retakes, missing data, authorization, recalculation idempotency, and PostgreSQL aggregation behavior.
