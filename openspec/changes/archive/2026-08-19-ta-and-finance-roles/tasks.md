## 1. Roles & Policies

- [x] 1.1 Add `Finance` and `TeachingAssistant` constants in `src/OpenLearning.Auth/Roles.cs`
- [x] 1.2 Add `RequireFinance`, `RequireTeachingAssistant`, `RequireFinanceOrAdmin` policy constants
- [x] 1.3 Update `src/OpenLearning.Auth/AuthModuleExtensions.cs` (or equivalent) to register the new policies in `AddAuthorization`
- [x] 1.4 Update `src/OpenLearning.Data/DbSeeder.cs` to seed the two new roles alongside the existing three

## 2. ClassAssignment Lookup (interface only)

- [x] 2.1 Define `IClassAssignmentLookup` in `OpenLearning.Auth` (or a small shared project): `Task<bool> IsAssignedAsync(string userId, int classGroupId); Task<IReadOnlyList<int>> ListAssignedClassIdsAsync(string userId);`
- [x] 2.2 Provide a default `NullClassAssignmentLookup` that returns `false` / empty so the change is shippable before `class-groups` lands

## 3. Finance Surface Migration

- [x] 3.1 Replace `[Authorize(Roles = Roles.Admin)]` with `[Authorize(Policy = Policies.RequireFinanceOrAdmin)]` on:
  - `Pages/Admin/Orders.cshtml(.cs)`
  - `Pages/Admin/Refunds.cshtml(.cs)`
  - `Pages/Admin/Reconciliation.cshtml(.cs)`
  - `Pages/Admin/Withdrawals.cshtml(.cs)`
  - `Pages/Admin/Coupons.cshtml(.cs)`
- [x] 3.2 Keep `[Authorize(Roles = Roles.Admin)]` on `Pages/Admin/Users.cshtml`, `Pages/Admin/Courses.cshtml`, `Pages/Admin/Categories.cshtml`, `Pages/Admin/Tags.cshtml`, `Pages/Admin/MembershipPlans.cshtml`, `Pages/Admin/System.cshtml`, `Pages/Admin/Operations.cshtml`, `Pages/Admin/Logs/*`, `Pages/Admin/InstructorApplications.cshtml`, `Pages/Admin/Identities.cshtml`

## 4. TA Pages (read-mostly)

- [x] 4.1 Create `Pages/TA/Index.cshtml(.cs)` listing class groups the TA is assigned to; policy `RequireTeachingAssistant`
- [x] 4.2 Create `Pages/TA/Roster.cshtml(.cs)` showing the assigned class's roster + per-student progress; gates by `IClassAssignmentLookup.IsAssignedAsync`
- [x] 4.3 Create `Pages/TA/Reminders.cshtml(.cs)` letting the TA send a reminder notification to selected students of the class; reuses `NotificationService`
- [x] 4.4 Confirm a TA cannot reach `/Courses/{id}/Edit` (existing policy unchanged)

## 5. Admin Role Management

- [x] 5.1 Extend `Pages/Admin/UserDetail.cshtml(.cs)` to show Finance and TeachingAssistant toggles in addition to Instructor
- [x] 5.2 On toggle, call `UserManager.AddToRoleAsync` / `RemoveFromRoleAsync` and write an operation-log entry
- [x] 5.3 Confirm the user gains/loses access on the next request (no restart required)

## 6. Navigation

- [x] 6.1 Update the navigation-chrome default menu to include a TA group (`助教工作台` — assigned classes, reminders) and a Finance group (`财务工作台` — orders, refunds, reconciliation, withdrawals, invoices)
- [x] 6.2 Confirm a Finance user does not see Admin-only sidebar groups

## 7. Verification

- [x] 7.1 `dotnet build OpenLearning.sln` is 0 warnings / 0 errors
- [x] 7.2 HTTP smoke tests via `curl -c/-b` against `http://localhost:5096`:
  - Admin promotes `student@openlearning.dev` to Finance → user can reach `/Admin/Refunds`
  - Admin promotes the same user to TeachingAssistant → user reaches `/TA`, sees assigned classes
  - TA cannot reach `/Courses/{id}/Edit`
  - Finance cannot reach `/Admin/Users`
- [x] 7.3 Confirm an operation-log entry exists for each role add/remove
- [x] 7.4 Confirm `IClassAssignmentLookup.NullClassAssignmentLookup` returns `false` for all users (TA sees empty class list until `class-groups` ships)