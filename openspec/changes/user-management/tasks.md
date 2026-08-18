# User Management & Instructor Onboarding — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.UserManagement` class library and add it to the solution
- [ ] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [ ] 1.3 Add `IsSuspended` to `ApplicationUser`; add `InstructorApplication` entity + config
- [ ] 1.4 Register assembly scanning in `ApplicationDbContext` and `AddUserManagementModule` in `Program.cs`

## 2. Services

- [ ] 2.1 Implement `UserManagementService`: search/list users, user detail (roles, enrollments, courses), set roles, suspend/reactivate
- [ ] 2.2 Implement `InstructorApplicationService`: submit/replace application, list pending, approve (grant role), reject
- [ ] 2.3 Add suspension enforcement (authorization check blocking learning/teaching/chat for suspended users)

## 3. UI

- [ ] 3.1 Admin users list/search page + user detail page
- [ ] 3.2 Admin instructor-application review page
- [ ] 3.3 Applicant apply page (`/ApplyInstructor`) with status display

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Run `dotnet build` and start the app
- [ ] 4.3 Verify: search, role grant/revoke, suspension, apply → approve flow end-to-end
