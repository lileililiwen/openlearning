# Ratings & Reviews — Design

## Context

There is no student feedback surface. This change adds per-course ratings/reviews with aggregate display and moderation, following the modular-monolith pattern.

## Goals

- Enrolled students leave one rating + optional comment per course.
- Aggregate rating (average + count) shown on cards and the course detail page.
- Owners can read reviews; admins can remove abusive ones.

## Non-Goals

- No review editing after submission (re-submission allowed instead).
- No weighted ratings or verification badges.
- No review replies from owners (deferred).

## Decisions

### D1: New `OpenLearning.Ratings` module
`Review { Id, CourseId, UserId, Rating (1-5), Comment (nullable, max 2000), CreatedAt }` with a unique index on `(CourseId, UserId)` so one review per student per course. `ReviewService` provides: submit (enrolled-only, upsert on duplicate), aggregate (`GetRatingAsync` → average + count), list for owner, remove for admin.

### D2: Aggregation on demand
`Average`/`Count` computed with an aggregate query on demand. This feeds the catalog sort in `course-discovery` and the card badges. No denormalized rating column (avoid drift); revisit if query volume demands it.

### D3: Moderation
Admins can delete any review. Owners can see reviews but not delete (avoid conflict of interest); a reported/rejected flag is deferred.

## Risks / Trade-offs

- **On-demand aggregates in the catalog list** → One grouped query per page of cards; acceptable, add a cached/denormalized rating later if needed.
- **Gaming (self-rating)** → Rating requires an active enrollment, matching the purchase/learning requirement.

## Migration Plan

One migration creates `Reviews`.

## Open Questions

- Should a student be able to change their rating after updating? MVP: re-submitting replaces the review (upsert).
