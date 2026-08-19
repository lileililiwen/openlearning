## Why

Ratings and reviews exist, but the reference system's Interactive Community module lists "follow-up comments" as part of Course Comments & Reviews. Conversations under a review (e.g. the instructor replying to a student's review) are currently impossible.

## What Changes

- Threaded comments under a review: any enrolled student or the course owner can comment on a review; comments are visible with the review.
- Instructor comments are flagged; admins can remove comments (moderation).
- Comments do not affect the rating aggregate.

## Capabilities

### New Capabilities
- `review-followups`: threaded comments under course reviews.

### Modified Capabilities

- `ratings-reviews`: reviews gain a comment thread; admin moderation extends to comments.

## Impact

- `ReviewComment { Id, ReviewId, AuthorId, Body, CreatedAt }` in `OpenLearning.Ratings`.
- `ReviewService` gains `AddCommentAsync` (enrolled-or-owner), `ListCommentsAsync`, `DeleteCommentAsync` (admin/author).
- Course details renders comments under each review; review moderation page shows comment removal.
