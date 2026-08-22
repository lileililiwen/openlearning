# Learner Notes — Design

## Context

Notes are user-owned records that may reference content without becoming owned by that content aggregate.

## Goals

- Capture notes at course, lesson, resource, and media-time contexts.
- Keep notes private and searchable.
- Export and permanently delete a learner's notes.

## Non-Goals

- Shared annotations, instructor feedback, or collaborative documents.
- Offline synchronization; mobile support can consume the notes API later.

## Decisions

### D1: User-owned note aggregate

Store `LearnerNote` with owner ID, optional context IDs, optional media offset, sanitized body, tags, and timestamps. Validate referenced content visibility at creation and display.

### D2: Safe formatting

Accept a constrained Markdown subset, store source text, and render through an allowlist sanitizer.

### D3: Ownership first

All reads and mutations begin with owner scope; unknown and foreign IDs have indistinguishable not-found behavior.

## Risks / Trade-offs

- Referenced content may be removed; notes remain accessible with a deleted-context label.
- Full-text search starts with PostgreSQL indexing and can move behind the search service later.

## Migration Plan

Add notes and tags tables; no existing data conversion.
