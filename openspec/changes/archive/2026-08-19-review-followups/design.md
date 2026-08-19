# Review Follow-ups — Design

## Context

Reviews are one-way. Follow-up comments let instructors respond publicly and let students clarify, which is standard in the reference system.

## Goals

- Enrolled students and the course owner can comment on reviews.
- Instructor comments are marked.
- Admins can remove abusive comments.

## Non-Goals

- No nested replies beyond one level (comments on comments deferred).
- No comment editing.
- No notifications per comment (a notification on the first comment is enough; volume capped).

## Decisions

### D1: `ReviewComment` in `OpenLearning.Ratings`
`ReviewComment { Id, ReviewId, AuthorId, Body, CreatedAt }` with index on `ReviewId`; `Review` gains a `Comments` collection. Comment body max 1000 chars; unique `(ReviewId, AuthorId, Body)` guard against duplicate spam.

### D2: Permissions
Add comment: any enrolled student in the course or the course owner. Read: same audience as the review itself. Delete: the author, the course owner, or an admin. Moderation deletion by admin mirrors the existing review-removal UI.

## Risks / Trade-offs

- **Thread volume** → Single-level comments keep the model simple; admin delete covers abuse.

## Migration Plan

One migration creates `ReviewComments`.

## Open Questions

- Should the instructor's comment pin to top? MVP: order by created date (oldest first).
