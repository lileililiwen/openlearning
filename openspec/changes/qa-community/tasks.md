# Q&A & Community — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Community` class library, add to solution, add references (Auth, CourseManagement, Enrollment, EF Core)
- [ ] 1.2 Add `Question`, `QuestionReply`, `Post`, `PostReply` entities + configs
- [ ] 1.3 Implement `CommunityService` (ask/reply, post/reply, list, admin delete)
- [ ] 1.4 Register assembly scanning + `AddCommunityModule`

## 2. UI

- [ ] 2.1 Course Q&A page (list questions, ask, reply, instructor-answer badge)
- [ ] 2.2 Course community page (posts + replies)
- [ ] 2.3 Gate read/write by enrollment/ownership/admin; draft-course restriction

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: ask/reply, post/reply, instructor badge, non-enrolled denied, admin delete, duplicate-reply guard
