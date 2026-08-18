# Content Review — Design

## Context

The platform has moderation primitives (admin delete on courses/reviews) but no review workflow or report queue.

## Goals

- Courses pass through an admin review before/at publish.
- Users can report content; admins resolve reports by removing or clearing.
- Removed content is hidden everywhere immediately.

## Non-Goals

- No auto-moderation/ML.
- No appeal workflow for content owners (resolved reports are final in MVP).
- No per-report notifications to reporters.

## Decisions

### D1: Course review state
`Course.Status` enum gains `UnderReview = 2`. Instructor "Publish" sets `UnderReview` (unless the course was already published); admin approves (→ Published) or rejects (→ Draft + `ReviewNote`). Draft courses are never visible to students. Admin review list shows pending courses.

### D2: Content reports
`ContentReport { Id, ContentType (Review/Comment/Question/Post/Reply), ContentId, ReporterId, Reason, Status (Open/Resolved/Rejected), ResolvedBy, ResolvedAt }`. Reporting is available to signed-in users (self-report prevention: cannot report own content). Admin queue lists open reports with the reported content inline; "Remove content" hides it (deletes or flags `IsHidden` on the content entity) and resolves the report; "Dismiss" rejects it.

### D3: Hidden content
`IsHidden` flag on review/comment/question/post/reply entities; all read queries filter `!IsHidden`. Deleting is also acceptable where the content type already supports deletion.

## Risks / Trade-offs

- **Review gating on publish** → Adds an extra step for instructors; unavoidable for moderation. Instructors see the status and review note on the course page.
- **Report spam** → One open report per (content, reporter) unique index.

## Migration Plan

One migration adds the status value and the `ContentReports` table plus `IsHidden` flags.

## Open Questions

- Should edits by instructors reset a published course to UnderReview? MVP: no; only the publish action triggers review when not already published.
