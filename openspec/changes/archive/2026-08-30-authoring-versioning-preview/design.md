# Design: Authoring Versioning and Preview

## Context

Existing course management supports draft and published states. This change makes the learner-visible state explicit so an instructor can continue editing safely after publication.

## Decisions

- Store a monotonically numbered revision per course; a revision contains the course metadata and ordered content snapshot needed for delivery.
- Keep one mutable draft and one active published revision. Publishing atomically validates and promotes the draft.
- Preview uses the draft for its owner only and is never reachable through public catalog or learner delivery queries.
- Rollback creates a new draft from a prior published revision instead of mutating history.
- Record actor, timestamp, revision number, validation result, and lifecycle action in an audit record.

## Failure and security

Invalid content remains unpublished with actionable field-level errors. Non-owners receive denial without draft existence leakage. Concurrent publish requests use a concurrency token and only one can succeed.

## Verification

Unit tests cover revision transitions and validation; PostgreSQL integration tests cover atomic promotion and uniqueness; Razor smoke tests cover owner/non-owner and learner visibility paths.
