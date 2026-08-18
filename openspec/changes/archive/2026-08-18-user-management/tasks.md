# User Management & Instructor Onboarding — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.UserManagement` class library and add it to the solution
- [x] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [x] 1.3 Add `IsSuspended` to `ApplicationUser`; add `InstructorApplication` entity + config
- [x] 1.4 Register assembly scanning in `ApplicationDbContext` and `AddUserManagementModule` in `Program.cs`

## 2. Services

- [x] 2.1 Implement `UserManagementService`: search/list users, user detail (roles, enrollments, courses), set roles, suspend/reactivate
- [x] 2.2 Implement `InstructorApplicationService`: submit/replace application, list pending, approve (grant role), reject
- [x] 2.3 Add suspension enforcement (authorization check blocking learning/teaching/chat for suspended users)

## 3. UI

- [x] 3.1 Admin users list/search page + user detail page
- [x] 3.2 Admin instructor-application review page
- [x] 3.3 Applicant apply page (`/ApplyInstructor`) with status display

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Run `dotnet build` and start the app
- [x] 4.3 Verify: search, role grant/revoke, suspension, apply → approve flow end-to-end
