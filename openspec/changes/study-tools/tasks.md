# Study Tools — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.StudyTools` class library, add to solution, add references (Auth, CourseManagement, Progress, EF Core)
- [ ] 1.2 Add `LessonNote`, `StudyCheckIn`, `LessonDownload` entities + configs
- [ ] 1.3 Implement `StudyToolService` (notes upsert/export, check-in, calendar/report, downloads)
- [ ] 1.4 Register assembly scanning + `AddStudyToolsModule`

## 2. UI

- [ ] 2.1 Lesson notes panel (save, edit, export `.md`) on lesson pages
- [ ] 2.2 `/Study` page: check-in button, month calendar, study report (duration, streak, completed)
- [ ] 2.3 Lesson downloads list (gated by enrollment + `IsAllowed`)

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: note save/export, check-in once per day, calendar/report render, downloads gated
