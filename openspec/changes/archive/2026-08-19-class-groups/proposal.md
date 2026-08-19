## Why

The brief treats 助教/班主任 (TA) as a first-class role with 督学、催交作业、社群答疑、跟进学习进度 responsibilities, all of which are scoped to a *class* (班级 / 班级群 / 期次). Today the system has a course (课程) but no notion of a class term under it; `qa-community` adds class-group posts but no entity, and `teacher-roster` lists every enrollment of the course, not a cohort. We add a `ClassGroup` (a course's term/cohort) so a TA can be assigned to a single class and have scoped rights, and so 班级报表 / 班级薄弱知识点 can mean something concrete.

## What Changes

- A `ClassGroup` belongs to a course and has a name, start, end, capacity, and a status (Upcoming/Open/Closed).
- A class has `ClassAssignment { ClassGroupId, UserId, Role }` rows for the Instructor-of-record, TAs, and (read-only) observers.
- A learner is enrolled in a class via `Enrollment.ClassGroupId` (nullable — non-class enrollments keep working).
- TA pages (`ta-and-finance-roles`) become meaningful: a TA sees only their assigned class(es), their roster, and their progress dashboard.
- Class-scoped Q&A: posts and questions in `qa-community` can be tagged with a `ClassGroupId` so a TA only sees class traffic by default (course-wide view remains for the course owner).
- Class-scoped announcements: notifications tagged with `ClassGroupId` only reach members of that class.

## Capabilities

### New Capabilities

- `class-groups`: class groups under a course, class assignments, class-scoped roster / progress / Q&A / announcements, class lifecycle (Upcoming/Open/Closed), class reports.

### Modified Capabilities

- `course-management`: course owners can create class groups under their courses.
- `enrollment`: an enrollment can attach to a `ClassGroupId` (nullable).
- `teacher-roster`: a class-scoped roster view per class group; the existing course-level roster is preserved.
- `qa-community` (pending): questions and posts gain an optional `ClassGroupId`; default visibility for class-scoped items is class members.
- `notifications`: class-scoped announcements target only enrolled students of that class.
- `ta-and-finance-roles` (proposed): `IClassAssignmentLookup` returns the TA's assigned classes from this module.

## Impact

- New `OpenLearning.Classes` module: `ClassGroup { Id, CourseId, Name, StartsAt, EndsAt, Capacity, Status, CreatedAt }`, `ClassAssignment { Id, ClassGroupId, UserId, Role (enum: Instructor/TA/Observer), AssignedAt }`.
- EF migration `AddClassGroups` adds the two tables and an `Enrollment.ClassGroupId` FK column.
- Services: `ClassGroupService` (CRUD owner-gated), `ClassAssignmentService` (assign/revoke TA), `ClassRosterService` (per-class roster with progress).
- Pages: `Pages/Courses/Classes/Index.cshtml(.cs)` (list/create class), `Pages/Courses/Classes/Manage.cshtml(.cs)` (assign TAs, set capacity, close); `Pages/TA/Roster.cshtml(.cs)` re-implemented to read from this module; new `Pages/TA/Class/Index.cshtml(.cs)` overview.
- Follows §2.1 modular monolith; no module references `OpenLearning.Data`.
- `notifications` gains an optional `ClassGroupId` on the `Notification` entity; covered by `notification-events-extensions`.