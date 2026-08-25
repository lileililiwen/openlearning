# Surveys & Polls — Design

## Context

Course feedback, live-session polling, and quick comprehension checks all need instructor-authored questionnaires. Existing features cover adjacent needs only: ratings-reviews (course reviews), qa-community (questions). Moodle ships Feedback + Choice; Canvas ships surveys — both as non-graded activity types. The design goal is a small, safe, non-graded survey engine.

## Goals

- Four question types covering feedback and polling needs.
- One-response enforcement with windows and scope-based eligibility.
- Anonymity enforced at storage level for anonymous surveys.
- Aggregate-only author results until close unless live results are enabled.

## Non-Goals

- Branching/conditional questions, question banks reuse, quiz-style scoring.
- Cross-survey analytics or export (grade-export/async-io-jobs can source it later).
- Scheduled recurring surveys.

## Decisions

### D0: New module following the fixed pattern

`OpenLearning.Survey` per §2. Scope is a nullable course reference: null = platform scope (Admin-managed), set = course scope (owner-managed via existing ownership checks; eligibility via enrollment service).

### D1: Anonymity by unlinking at write time

Anonymous responses persist without a user-id column value (null) plus an opaque per-user submission token in the session to enforce one-response-per-user without retaining identity. Attributed responses store the user id directly. This makes anonymity structural rather than a display filter.

### D2: Aggregates computed on read

Result pages compute counts/percentages from stored answers — consistent with gradebook and competency read-time precedents. Open-text answers are listed verbatim only for attributed surveys; anonymous open text is listed without any ordering that could correlate respondents.

### D3: Live results default off

Authors see response counts while open; full aggregates unlock at close or when live results are explicitly enabled at creation.

## Risks / Trade-offs

- Small-cohort anonymous surveys can be de-anonymized by cross-referencing timing; mitigated by hiding timestamps from authors.
- Opaque token approach stores no identity but still prevents duplicates; acceptable trade-off versus full identity linkage.

## Migration Plan

Add survey, question, response, and answer tables. No backfill; optional notification template wiring reuses existing events.
