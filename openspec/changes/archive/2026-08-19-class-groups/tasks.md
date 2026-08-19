## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Classes` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.CourseManagement`, `OpenLearning.Enrollment`, `OpenLearning.Notifications`, `OpenLearning.QA` (never `OpenLearning.Data`)
- [x] 1.2 Add `ClassGroup { Id, CourseId, Name, StartsAt, EndsAt, Capacity?, Status (Upcoming/Open/Closed), CreatedAt }` and `ClassAssignment { Id, ClassGroupId, UserId, Role (enum: Instructor/TA/Observer), AssignedAt }` entities + configs
- [x] 1.3 Add `Enrollment.ClassGroupId` (nullable FK) — extend `EnrollmentConfiguration`
- [x] 1.4 EF migration `AddClassGroups` via `dotnet ef migrations add AddClassGroups --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 1.5 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 2. Service Layer

- [x] 2.1 Implement `ClassGroupService` (CRUD owner-gated, `EffectiveStatus` property returns `Open`/`Closed` based on time)
- [x] 2.2 Implement `ClassAssignmentService` (assign/revoke, unique-by-(class,user,role), idempotent revoke)
- [x] 2.3 Implement `ClassRosterService` (per-class roster with progress + last activity + outstanding assignments)
- [x] 2.4 Implement `IClassAssignmentLookup` (used by `ta-and-finance-roles`) with `IsAssignedAsync`, `ListAssignedClassIdsAsync`

## 3. Pages

- [x] 3.1 `Pages/Courses/Classes/Index.cshtml(.cs)` — list classes for a course (Instructor/Admin)
- [x] 3.2 `Pages/Courses/Classes/Create.cshtml(.cs)` and `Edit.cshtml(.cs)` — owner-gated
- [x] 3.3 `Pages/Courses/Classes/Manage.cshtml(.cs)` — assign TAs, set capacity, close/open
- [x] 3.4 `Pages/Courses/Classes/Roster.cshtml(.cs)` — class roster with progress, CSV export button
- [x] 3.5 `Pages/Courses/Enrollments/EnrollIntoClass.cshtml(.cs)` — Admin/Instructor enrolls a student into a class (sets `Enrollment.ClassGroupId`)

## 4. TA Dashboard

- [x] 4.1 `Pages/TA/Index.cshtml(.cs)` — list assigned classes (uses `IClassAssignmentLookup`)
- [x] 4.2 `Pages/TA/Class/Roster.cshtml(.cs)` — class roster view
- [x] 4.3 `Pages/TA/Class/Reminders.cshtml(.cs)` — send class-scoped reminders
- [x] 4.4 Replace the placeholder `Pages/TA/Roster.cshtml(.cs)` shipped by `ta-and-finance-roles` with a redirect to `/TA/{classId}/Roster`

## 5. Q&A Integration

- [x] 5.1 Add `ClassGroupId` to `Question` / `Post` entities in `qa-community` (or shared with this module's migration)
- [x] 5.2 Update `CommunityService` so class-scoped posts/questions are returned only to class members when `ClassGroupId IS NOT NULL`
- [x] 5.3 Update `Pages/Courses/Qa/Index.cshtml(.cs)` and `Pages/Courses/Community/Index.cshtml(.cs)` to add a "本班 / 全部" tab toggle for class members
- [x] 5.4 Ensure non-class members still see course-wide items

## 6. Notifications

- [x] 6.1 Add `Notification.ClassGroupId` (nullable FK) — covered by `notification-events-extensions`
- [x] 6.2 Add `NotificationService.SendClassAnnouncementAsync(classGroupId, title, body, senderId)` which targets only enrolled students of that class
- [x] 6.3 Add an "Announce" action on `Pages/Courses/Classes/Manage.cshtml(.cs)` (Instructor or assigned TA)

## 7. Build & Verify

- [x] 7.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 7.2 HTTP smoke tests:
  - Create a class under a course; assign a TA; enroll a student into the class
  - Verify the TA sees the class on `/TA`, denied on another class
  - Verify the student sees only class-scoped Q&A when in the class tab
  - Verify class-scoped announcement reaches only class students
  - Verify CSV export downloads with the expected columns
  - Verify capacity is enforced
  - Verify status flips to `Open` automatically when `StartsAt` passes; flips to read-only when `EndsAt` passes
- [x] 7.3 Verify the existing `Course.Roster` page still works for courses with no classes (legacy mode)