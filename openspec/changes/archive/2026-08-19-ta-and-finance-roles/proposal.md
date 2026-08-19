## Why

The brief calls for 助教/班主任 and a dedicated 财务角色 in addition to the existing Student / Instructor / Admin. Today `Roles.cs` only knows `Admin`, so finance-only and TA-only work is conflated with general administration. Concretely:
- 财务 (Finance) should own refund approval, invoice issuance, reconciliation, settlement review, but should NOT see admin console pages unrelated to finance.
- 助教 / 班主任 (TA) should be able to manage a class group (催办、跟进进度、社群答疑) but NOT publish courses, change site settings, or alter other instructors' courses.

We add the two roles, scope the existing finance and class-management surfaces to them, and refactor the admin pages where the right boundary is "Finance" rather than "Admin".

## What Changes

- Add `Finance` and `TeachingAssistant` to `Roles`.
- Add policies `RequireFinance`, `RequireTeachingAssistant`, and a composite `RequireFinanceOrAdmin` for surfaces both should see.
- Restrict `finance-admin` pages (orders, refunds, reconciliation, withdrawals, invoices) to `RequireFinanceOrAdmin` so a Finance-only user can use them without becoming an Admin.
- Add a `TA` scope: TAs assigned to a class group can view that group's roster, post announcements, send reminders, view that group's progress; TAs cannot edit the course content.
- Update `navigation-chrome`'s sidebar groups (which we treat as the source of truth here for the role-aware sidebar) so Finance and TA each see their own group list.

## Capabilities

### New Capabilities

- `ta-and-finance-roles`: TA and Finance roles, their policies, the surfaces they own, and the cross-role boundary.

### Modified Capabilities

- `user-management`: Admin can assign/revoke `Finance` and `TeachingAssistant` (alongside existing `Instructor`); suspension applies to all roles.
- `lms-core`: the role-aware navigation shell exposes a TA sidebar (assigned classes, reminders) and a Finance sidebar (orders, refunds, reconciliation, withdrawals, invoices).
- `finance-admin`: the policy becomes `RequireFinanceOrAdmin` rather than `RequireAdmin`.

## Impact

- `src/OpenLearning.Auth/Roles.cs`: add `public const string Finance = "Finance"; public const string TeachingAssistant = "TeachingAssistant";` and matching `Policies`.
- `OpenLearning.Web/Program.cs`: register the two new roles in `DbSeeder` and policies in `AddAuthorization`.
- Razor pages: `[Authorize(Policy = Policies.RequireFinanceOrAdmin)]` replaces `[Authorize(Roles = Roles.Admin)]` on finance-admin pages; the new TA pages under `/TA/{classId}/...` get `RequireTeachingAssistant`.
- Class-group assignment: `class-groups` change adds the `ClassAssignment { ClassGroupId, UserId, Role }` table; this change just references it for the TA scope. If `class-groups` hasn't shipped yet, this change ships a stub `IClassAssignmentLookup` that returns "not assigned" so TAs see no class until the other change lands.
- New admin actions: an Admin can promote/demote users to/from `Finance` and `TeachingAssistant` on the existing `/Admin/Users/{id}` page (no new page).