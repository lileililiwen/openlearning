## Why

Admins can manage users and delete courses/reviews, but there is no structured content-review workflow. The reference system's Admin Backend requires Content Review: course review, comment review, Q&A review, and violation handling.

## What Changes

- Course review: new/published courses enter a review state; admins approve or reject with feedback.
- Comment/review review: users can report reviews, review comments, and Q&A content; admins see a report queue and remove content or dismiss reports.
- Violation handling: reported content is flagged and hidden pending review.

## Capabilities

### New Capabilities
- `content-review`: course review workflow and content-report moderation.

### Modified Capabilities

- `course-management`: publish lifecycle gains a Review state.
- `ratings-reviews`: reviews/comments can be reported and hidden.
- `qa-community`: Q&A/posts can be reported and hidden.

## Impact

- New `OpenLearning.Review` module: `ContentReport { Id, ContentType, ContentId, ReporterId, Reason, CreatedAt, Status, ResolvedBy, ResolvedAt }`.
- `ContentReviewService` (report, queue, resolve, hide/unhide).
- `Course.Status` gains `UnderReview`; admin course list gains review actions; report queues per content type.
