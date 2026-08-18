# Review Follow-ups — Tasks

## 1. Data & Service

- [ ] 1.1 Add `ReviewComment` entity + config in the Ratings module; add `Comments` to `Review`
- [ ] 1.2 Extend `ReviewService`: add/delete/list comments with permission checks

## 2. UI

- [ ] 2.1 Comment thread under each review on course details (enrolled/owner can comment; instructor badge)
- [ ] 2.2 Admin comment removal in the review moderation page

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: comment add, instructor badge, non-enrolled denied, admin delete, duplicate guard
