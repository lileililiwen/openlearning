# Study Tools — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.StudyTools` class library, add to solution, add references (Auth, CourseManagement, Progress, EF Core)
- [x] 1.2 Add `LessonNote`, `StudyCheckIn`, `LessonDownload` entities + configs
- [x] 1.3 Implement `StudyToolService` (notes upsert/export, check-in, calendar/report, downloads)
- [x] 1.4 Register assembly scanning + `AddStudyToolsModule`

## 2. UI

- [x] 2.1 Lesson notes panel (save, edit, export `.md`) on lesson pages
- [x] 2.2 `/Study` page: check-in button, month calendar, study report (duration, streak, completed)
- [x] 2.3 Lesson downloads list (gated by enrollment + `IsAllowed`)

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: note save/export, check-in once per day, calendar/report render, downloads gated
