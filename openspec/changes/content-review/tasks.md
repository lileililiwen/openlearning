# Content Review — Tasks

## 1. Data & Service

- [ ] 1.1 Add `UnderReview` to `CourseStatus`; add `ReviewNote` to `Course`; add `IsHidden` flags to content entities
- [ ] 1.2 Add `ContentReport` entity + config; implement `ContentReviewService` (report, queue, resolve, hide)
- [ ] 1.3 Course publish sets UnderReview (when not already published); admin approve/reject actions

## 2. UI

- [ ] 2.1 Admin course review list (pending) + approve/reject with note
- [ ] 2.2 Report buttons on reviews/comments/Q&A; admin report queue with inline content
- [ ] 2.3 Hidden content filtered from all read queries

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: publish → UnderReview → approve publishes / reject hides, report → remove hides everywhere, dismiss, non-admin denied
