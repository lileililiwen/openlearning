# Assignments — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Assignments` class library, add to solution, add references (Auth, CourseManagement, Enrollment, EF Core)
- [x] 1.2 Add `Assignment` + `AssignmentSubmission` entities + configs
- [x] 1.3 Implement `AssignmentService` (CRUD owner-gated, submit, list, grade)
- [x] 1.4 Register assembly scanning + `AddAssignmentsModule`

## 2. UI

- [x] 2.1 Assignment list on course (enrolled/owner gated) + create/edit/delete for owner
- [x] 2.2 Submit page (text + optional file) for enrolled students
- [x] 2.3 Submissions page (instructor): list students, grade with score + feedback
- [x] 2.4 Student view of grade/feedback; resubmit per policy
- [x] 2.5 "Assignments due" indicator on student dashboard

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: create/assign, submit, grade, feedback visible, resubmit rules, non-owner denied
