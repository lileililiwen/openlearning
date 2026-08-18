## Why

Roles are only assignable via seeding today: admins cannot manage users, and there is no way for a user to become an instructor. Without user management and an instructor onboarding path, the platform cannot grow beyond its seed data.

## What Changes

- **Admin user management**: list/search users, view a user's detail (roles, enrollments, courses), assign/revoke roles (e.g., promote a Student to Instructor), and suspend/reactivate accounts.
- **Instructor onboarding**: users can submit an instructor application; admins approve or reject it. Approval grants the `Instructor` role and notifies the applicant.
- New `OpenLearning.UserManagement` module (application records + services) following the modular-monolith pattern.

## Capabilities

### New Capabilities
- `user-management`: Admin CRUD-lite over users — search, role assignment, suspension.
- `instructor-onboarding`: Instructor application and admin approval workflow.

### Modified Capabilities

None.

## Impact

- New module `OpenLearning.UserManagement` referencing Auth, CourseManagement, Enrollment; entities `InstructorApplication` (and a `Suspended` flag on `ApplicationUser` or a separate table).
- Admin pages under `Pages/Admin/` (users list/detail) and application-review page.
- New `Pages/ApplyInstructor.cshtml` for applicants.
- Role grant/revoke uses `UserManager` in the Auth module; suspension is enforced by an authorization check.
- No changes to existing capabilities.
