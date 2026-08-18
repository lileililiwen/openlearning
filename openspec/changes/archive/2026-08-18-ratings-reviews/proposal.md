## Why

Students cannot signal course quality, and new visitors cannot judge it. Ratings and reviews are the primary social proof for a course platform and feed the discovery sort ("rating").

## What Changes

- Enrolled students can rate a course (1–5 stars) and leave one review per course.
- Courses show an aggregate rating (average + count) on cards and the detail page.
- Course owners see reviews for their courses; admins can remove inappropriate reviews (moderation).

## Capabilities

### New Capabilities
- `ratings-reviews`: student ratings and reviews with aggregate display, owner visibility, and admin moderation.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Ratings` module: `Review { Id, CourseId, UserId, Rating (1-5), Comment, CreatedAt }`, unique per (course, user); `ReviewService` (submit, aggregate, list for owner, moderation).
- Course cards/detail show the aggregate rating (consumed by `course-discovery` sorting).
- No changes to existing capabilities.
