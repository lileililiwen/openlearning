# Course Discovery — Design

## Context

The catalog renders all published courses newest-first with no search. This change adds discovery: richer course metadata, full-text search, category filter, sorting, and pagination.

## Goals

- Students can find courses by keyword, category, and ordering preference.
- Course cards communicate level, duration, rating, and price at a glance.

## Non-Goals

- No faceted multi-select filters (MVP: single category).
- No personalization/recommendation engine (dashboard suggests same-category courses only).
- No review writing here (see `ratings-reviews`).

## Decisions

### D1: Metadata on `Course`
`Level` (enum: Beginner/Intermediate/Advanced), `Duration` (string, e.g., "6 hours"), `Language`, `Prerequisites`, `LearningOutcomes` (long text). Optional fields; empty values render as "—". Rationale: simple columns, no join to a metadata table, consistent with the existing `Category` string approach. A managed category taxonomy is deferred to `platform-analytics`/admin settings.

### D2: Search implementation
PostgreSQL full-text via EF: use `EF.Functions.ILike` / `ToTsQuery` on title+description+category. MVP decision: `ILike` contains-search over `Title`, `Description`, `Category` — simple, indexable later. Rationale: avoids schema/trigger setup; adequate at MVP scale.

### D3: Catalog query helper
`CourseService.SearchAsync(search, category, sort, page, pageSize)` builds the filter chain. Sorts: newest (default), popular (enrollment count), price (low/high), rating (average — returns zero until `ratings-reviews` lands). Pagination via `Skip/Take` with a total count.

### D4: Card metadata
Cards show: title, category, level, duration, price/free badge, and rating placeholder (once ratings exist). Keep card height consistent (truncated description).

## Risks / Trade-offs

- **`ILike` full scan** → MVP scale acceptable; a real FTS index is a documented future step.
- **Sort by rating before ratings exist** → Returns all courses with equal (zero) rating; harmless.

## Migration Plan

One migration adds the metadata columns to `Courses`.

## Open Questions

- Category management (taxonomy) — deferred to admin settings work.
