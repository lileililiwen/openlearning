# User Management & Instructor Onboarding — Design

## Context

The Auth module seeds the three roles but exposes no admin tooling. This change adds an admin user-management surface and a self-service instructor application flow, both backed by a new `OpenLearning.UserManagement` module.

## Goals

- Admins can find any user and act on their account (roles, suspension).
- A user can apply to become an instructor; an admin approves/rejects.
- Role changes take effect immediately (no restart).

## Non-Goals

- No fine-grained permissions beyond the three roles.
- No self-service signup as instructor without approval.
- No password reset from admin console here (see `user-profiles`).

## Decisions

### D1: New `OpenLearning.UserManagement` module
`InstructorApplication { Id, UserId, Motivation, Status (Pending/Approved/Rejected), SubmittedAt, ReviewedAt, ReviewedBy }`. Services use `UserManager`/`RoleManager` (via Auth) and base `DbContext`. The `ApplicationUser` gains a `Suspended` flag (boolean) — or a separate `UserStatus` table. Decision: add `IsSuspended` to `ApplicationUser` for simplicity.

### D2: Suspension enforcement
Suspension is checked in a small authorization/behavior: suspended users cannot open lessons, take quizzes, or chat. Implementation options: a custom `AuthorizationHandler` that fails suspended users on sensitive policies, plus an in-app banner. Rationale: middleware-level enforcement is simpler than editing every page.

### D3: Role assignment via UserManager
Admin actions call `UserManager.AddToRoleAsync/RemoveFromRoleAsync`. Because roles back the existing policies, changes apply immediately on the next request.

### D4: Application workflow
The apply page writes a `Pending` application (one per user). The admin review page lists pending applications with the applicant's profile; approve → `AddToRoleAsync(Instructor)` + mark `Approved`; reject → mark `Rejected` with an optional reason. The applicant is notified via the notifications capability when it exists; until then a status shown on their apply page.

## Risks / Trade-offs

- **Admin misuse of role grants** → All admin actions require `Admin` role and are logged (audit trail column or a simple `AdminActionLog`; deferred to `platform-analytics` audit).
- **Suspension bypass via cached claims** → Claims are refreshed on each request via `SecurityStampValidator`, so suspension takes effect on the next request.

## Migration Plan

One migration adds `IsSuspended` to `AspNetUsers` and creates `InstructorApplications`.

## Open Questions

- Should instructor applications require review fields like experience/links? MVP: free-text motivation + display name.
- Should rejected applicants be able to re-apply? Yes (new application overwrites previous).
