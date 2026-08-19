# Q&A & Community — Design

## Context

Learners need to ask questions and interact within a course. Chat is ephemeral; Q&A and posts are persistent and searchable.

## Goals

- Enrolled students ask questions in a course Q&A and reply to questions.
- Instructors/owners can answer and their answers are marked.
- Class-group posts with replies, restricted to the course.

## Non-Goals

- No cross-course social feed.
- No upvoting/likes (deferred).
- No rich text/markdown beyond plain text with newlines.

## Decisions

### D1: New `OpenLearning.Community` module
`Question { Id, CourseId, AuthorId, Title, Body, CreatedAt }`; `QuestionReply { Id, QuestionId, AuthorId, Body, CreatedAt }`; `Post { Id, CourseId, AuthorId, Body, CreatedAt }`; `PostReply { Id, PostId, AuthorId, Body, CreatedAt }`. Indexes on `CourseId` + `CreatedAt`; unique index `(QuestionId, AuthorId, Body)` prevents duplicate identical replies (soft spam guard).

### D2: Access rules
Read: enrolled students, course owner, and admins. Write: enrolled students can ask/post/reply; owner/instructor replies on questions are flagged "Instructor answer". Non-enrolled visitors cannot read or write (draft courses: owner/admin only).

### D3: Moderation hooks
`CommunityService` exposes delete-by-admin (admin can remove any question/post/reply) so the `content-review` change can plug in later. Reported/violation flags deferred.

## Risks / Trade-offs

- **Spam/duplicates** → Unique duplicate-reply guard + length limits (title 200, body 4000).
- **Moderation** → Admin delete hooks included now; review workflows come with `content-review`.

## Migration Plan

One migration creates `Questions`, `QuestionReplies`, `Posts`, `PostReplies`.

## Open Questions

- Should Q&A be open to non-enrolled visitors read-only? MVP: no.
