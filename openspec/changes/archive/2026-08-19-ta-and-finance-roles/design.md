## Context

Today `Roles.cs` carries only the three roles inherited from the initial LMS MVP. The brief asks for two more — Finance and TeachingAssistant — without disturbing the existing Admin capabilities. We follow the §2 modular-monolith pattern: the role enum is centralised in `OpenLearning.Auth` (no migration needed — Identity roles are stored as strings); the policies are registered in `Program.cs`; the UI policies on each page are updated. The TA scope depends on a class-group entity from the `class-groups` change; we ship a thin `IClassAssignmentLookup` interface in this change so the dependency is one-way (`ta-and-finance-roles` depends on the interface, the concrete implementation lives in the `class-groups` module which registers it).

## Goals / Non-Goals

**Goals:**
- Add `Finance` and `TeachingAssistant` roles with minimal blast radius.
- Move the finance-only surfaces from "Admin only" to "Finance or Admin".
- Give TAs a read-mostly scoped view of assigned classes.
- Allow Admins to assign/revoke the two roles from the existing user detail page.

**Non-Goals:**
- A new TA dashboard (the navigation-chrome change will render a TA group in the sidebar; specific TA pages are minimal here — `class-groups` extends them).
- Replacing any finance workflow; the change is purely authorisation.
- Permission inheritance (e.g. Finance inheriting Admin's powers). The two roles stay orthogonal.

## Decisions

- **Two new role strings, no enum migration**. ASP.NET Core Identity roles are strings; we add to `Roles.cs` and the existing `DbSeeder.RoleNames` list. No DB schema change.
- **`RequireFinanceOrAdmin` as a new policy** that allows both, used by finance pages. Alternative: re-authorise the finance pages with `RequireAdmin || RequireFinance` (rejected — each page would need two attributes).
- **TA scope via class-group assignments**, not a global "TA of the platform". The `IClassAssignmentLookup.IsAssignedAsync(userId, classId)` is the single source of truth.
- **No cross-role elevation** — a TA is not implicitly an Instructor, a Finance is not implicitly an Admin. This keeps the audit trail clean.
- **Admin still sees every page** — Admin is a superset by policy, not by data.

## Risks / Trade-offs

- [Risk: a page is missed in the policy migration and stays Admin-only] → Mitigation: the change lists every page it touches in `tasks.md`; smoke tests exercise a Finance user end-to-end.
- [Risk: a TA attempts to edit a course via a stale URL] → Mitigation: every mutating course handler keeps an explicit ownership/policy check; server-side ownership check is unchanged.
- [Risk: TA scope depends on `class-groups` which may not be shipped yet] → Mitigation: `IClassAssignmentLookup` ships in this change with a default "no assignments" implementation. The `class-groups` change swaps in the real implementation via DI.
- [Risk: admins promote a user to multiple roles and lose oversight] → Mitigation: the user detail page lists all assigned roles with revoke buttons; an operation-log entry is written when a role is added or removed.

## Migration Plan

1. Add roles + policies; deploy.
3. Update the finance pages' policies; deploy.
4. Add TA pages (`/TA/{classId}/Roster`, `/TA/{classId}/Reminders`).
5. Verify a `Finance` user can complete an end-to-end refund approval.
6. Verify a `TA` user is blocked from course editing.

## Open Questions

- Should a Finance user be allowed to *create* other Finance users? Current decision: no — only Admins. Revisit if Finance team grows large enough to self-manage.
- Should TAs be able to send announcements to a class? Currently yes via the existing notifications module; the `class-groups` change wires it.