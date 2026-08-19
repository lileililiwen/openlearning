## Why

The platform has course chat and reviews but no structured Q&A or community. The reference system's Interactive Community module lists Q&A (ask, reply, follow-up, course Q&A section), class groups, announcements, and posts. Announcements already exist via notifications.

## What Changes

- Course Q&A: enrolled students ask questions; owners/instructors and enrolled students reply; follow-up threads.
- Community: per-course class groups (membership = enrollment) with text posts and replies.
- Q&A/community posts are visible within the course and feed review-style moderation (`content-review` change).

## Capabilities

### New Capabilities
- `qa-community`: course Q&A and class-group posts with replies.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Community` module: `Question { Id, CourseId, AuthorId, Title, Body, CreatedAt }`, `QuestionReply { Id, QuestionId, AuthorId, Body, CreatedAt }`, `Post { Id, CourseId, AuthorId, Body, CreatedAt }`, `PostReply { Id, PostId, AuthorId, Body, CreatedAt }`.
- `CommunityService` (ask/answer, post/reply, list by course, owner answers flagged).
- Pages under `Pages/Courses/Qa/` and `Pages/Courses/Community/`; enrollment/ownership gating.
