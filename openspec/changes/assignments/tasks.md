# Assignments — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Assignments` class library, add to solution, add references (Auth, CourseManagement, Enrollment, EF Core)
- [ ] 1.2 Add `Assignment` + `AssignmentSubmission` entities + configs
- [ ] 1.3 Implement `AssignmentService` (CRUD owner-gated, submit, list, grade)
- [ ] 1.4 Register assembly scanning + `AddAssignmentsModule`

## 2. UI

- [ ] 2.1 Assignment list on course (enrolled/owner gated) + create/edit/delete for owner
- [ ] 2.2 Submit page (text + optional file) for enrolled students
- [ ] 2.3 Submissions page (instructor): list students, grade with score + feedback
- [ ] 2.4 Student view of grade/feedback; resubmit per policy
- [ ] 2.5 "Assignments due" indicator on student dashboard

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: create/assign, submit, grade, feedback visible, resubmit rules, non-owner denied
