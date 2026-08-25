# Competency Framework — Design

## Context

The platform records *what learners finished* (progress, assignments, certificates) but not *what they can do*. Corporate buyers treat skills frameworks and gap analysis as the primary LMS capability, and Moodle/Canvas/360Learning all provide one. Existing trusted completion sources make automatic evidence cheap; the risk is letting skills data leak into academic or monetary state.

## Goals

- Versioned frameworks with hierarchical competencies and scales.
- Automatic evidence from mapped activity completions; manual evidence behind approval.
- Profiles and gap analysis for individuals and cohorts.
- Strict separation from grades, credits, graduation, certificates, and payments.

## Non-Goals

- Skills ontologies with 20k predefined skills or AI skill inference.
- Recertification/expiry cycles (candidate follow-up once certificates gain validity periods).
- Cross-tenant framework sharing.

## Decisions

### D0: New module consuming existing completions

`OpenLearning.Competency` follows the §2 module pattern. Evidence creation reads completion data via the progress-tracking and assignments modules' services — never by joining across module tables. Mapping rows store the competency version current at mapping time.

### D1: Framework versioning mirrors badge criteria precedent

Frameworks are append-versioned like gamification badge criteria: edits create a new version; earned evidence pins the version it satisfied. Archiving hides a framework from new mappings without deleting history.

### D2: Evidence sync is lazy and idempotent

The platform has no event bus (gamification uses trusted-event keys plus an explicit backfill; analytics uses ingestion). Evidence therefore materializes through `SyncEvidenceAsync`, which scans active mappings against trusted completion state (course at 100% lesson completion, assignment submission graded) and inserts missing rows keyed by a unique source key (`course:{id}:user:{uid}` / `assignment:{id}:user:{uid}`). Sync runs whenever a profile or gap view is opened and via an explicit Admin backfill, so evidence always exists at every observation point without duplicating records.

### D3: Approval is a two-state review on manual evidence only

Manual submissions enter `Pending`; Instructor/Admin approve/reject with a reason. Automatic evidence is pre-approved by construction. Reviewer authorization reuses course ownership checks plus Admin policy.

### D4: Gap analysis is computed at read time

Profiles and gap reports derive from stored evidence on request; no materialized attainment projection until scale demands it.

## Risks / Trade-offs

- Framework sprawl and stale mappings; archive + versioning keep history honest.
- Manual evidence can be gamed; attachments plus reviewer accountability bound the risk.
- Read-time gap computation may get slow for very large cohorts; acceptable now, projection later if needed.

## Migration Plan

Add framework, competency node, mapping, evidence, and review tables. No backfill of historical completions unless an Admin explicitly runs a previewed backfill.
