# Analytics Expansion — Design

## Context

Transactional tables can answer headline counts but are inefficient and ambiguous for time-series learning behavior. Analytics also increases privacy risk.

## Goals

- Define stable learning events and reproducible aggregates.
- Serve operator and instructor reports within their authorization scope.
- Minimize personal data and expose freshness/quality metadata.

## Non-Goals

- General-purpose BI query access, advertising profiles, or automated learner penalties.
- Replacing finance source-of-truth reports.

## Decisions

### D1: Allowlisted event envelope

Record event type, pseudonymous actor key, course/context IDs, occurred/received times, schema version, and allowlisted properties. Reject arbitrary payload fields.

### D2: Scheduled aggregates

Background jobs produce daily course/cohort/assessment/workload facts. Reports display last-successful refresh and never mix partial runs.

### D3: Privacy boundary

Apply role/course/tenant scope, suppress cohorts below a configured size, limit retention, audit exports, and omit raw identities from instructor exports unless needed for an existing teaching workflow.

## Risks / Trade-offs

- Late events revise historical aggregates; recomputation windows remain configurable.
- Pseudonymous events are still personal data when linked; retention and access controls remain mandatory.

## Migration Plan

Add event and aggregate tables, begin collection prospectively, and label reports with available-data start dates.
