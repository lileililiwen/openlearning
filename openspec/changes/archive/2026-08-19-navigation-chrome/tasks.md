## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Navigation` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth` (Roles + ApplicationUser) only — never `OpenLearning.Data`
- [x] 1.2 Define `MenuItem { Key, GroupKey, Label, Route, IconKey?, AllowedRoles (Flags), SortOrder, Hidden }` and `MenuGroup { Key, Label, SortOrder, IconKey? }` records in `Models/`
- [x] 1.3 Implement `MenuService` with `GetTreeAsync(IList<string> roles)` returning the merged tree (built-in defaults + JSON from `system-config`), and `SaveTreeAsync(MenuTree)` writing JSON back to the `navigation.menu.v1` key
- [x] 1.4 Implement `INavCounterProvider` interface and a registry that aggregates `(key, count)` from contributors (notifications, assignments, study)
- [x] 1.5 Implement `BreadcrumbService` reading `[Breadcrumb("home", "my-courses", ...)]` attributes from page-model types via reflection cache, plus `IBreadcrumbProvider` for dynamic ancestors
- [x] 1.6 Implement `NavPreferencesService` reading/writing the signed `nav.collapsed` cookie
- [x] 1.7 Register `AddNavigationModule` in `OpenLearning.Web/Program.cs` (one line)
- [x] 1.8 No EF migration required (menu stored in `system-config` JSON); confirm `dotnet build OpenLearning.sln` is 0 warnings / 0 errors

## 2. Layout Shell

- [x] 2.1 Create `Pages/Shared/_SidebarLayout.cshtml` (sidebar + topbar + main) and `_Sidebar.cshtml`, `_Topbar.cshtml`, `_Breadcrumbs.cshtml` partials
- [x] 2.2 Replace `Pages/Shared/_Layout.cshtml` with a thin wrapper that selects `_SidebarLayout` for normal pages and (later) the focused layout for the course-player route
- [x] 2.3 Update `Pages/_ViewStart.cshtml` to point at the new wrapper
- [x] 2.4 Add `wwwroot/css/site-chrome.css` for sidebar/topbar/breadcrumb styles, link it from the layout head; reuse the existing `site.css` for buttons/cards/tables
- [x] 2.5 Verify the existing top-bar items move cleanly: notification badge → sidebar Notifications (and top-bar fallback on narrow viewports), cart count → top-bar avatar area, profile link → avatar dropdown, sign-out button → dropdown

## 3. Menu Tree and Badges

- [x] 3.1 Seed the built-in default menu for Student, Instructor, and Admin (groups and items per the brief)
- [x] 3.2 Wire `INavCounterProvider` contributors: `NotificationsNavCounter` (uses existing `NotificationService.GetUnreadCountAsync`), `AssignmentsNavCounter` (open pending-grading count for the user; uses `Assignments` module services), `StudyTodoNavCounter` (uses `StudyTools` module services)
- [x] 3.3 Render the badge on each sidebar item by looking up the counter key registered for that item
- [x] 3.4 Smoke-test per role: Student, Instructor, Admin each see only their groups with correct badges (signed-in users with zero counters show no badge)

## 4. Breadcrumbs

- [x] 4.1 Annotate representative pages with `[Breadcrumb]` to cover at least: lesson page (`home > my-courses > course > module > lesson`), admin user list (`home > admin > users > list`), admin course list (`home > admin > courses`), instructor roster (`home > instructor > courses > roster`)
- [x] 4.2 Verify breadcrumb renders with chevron separators and that the last segment is not a link

## 5. Admin Menu Management

- [x] 5.1 Create `Pages/Admin/Menu/Index.cshtml(.cs)` with the policy `AdminMenuConfig`
- [x] 5.2 List groups (sortable) and items (sortable within group) with edit/add/disable controls
- [x] 5.3 Save changes via `MenuService.SaveTreeAsync`; reload the page and verify the sidebar reflects the change for each role
- [x] 5.4 Validate that non-Admin users get 403 on `/Admin/Menu`

## 6. Responsive and Cookies

- [x] 6.1 Implement cookie-backed collapse state with a default of expanded; verify the cookie is signed and HttpOnly
- [x] 6.2 Implement narrow-viewport collapse (default breakpoint 992px) and the top-bar overlay toggle
- [x] 6.3 Manually smoke-test at 360px, 768px, 1280px for each role

## 7. Verification

- [x] 7.1 Build clean (`dotnet build OpenLearning.sln` — 0 warnings / 0 errors)
- [x] 7.2 Per-role HTTP smoke tests via `curl -c/-b` against `http://localhost:5096`:
  - Student sees only student groups; instructor/admin groups absent
  - Instructor sees only instructor groups; admin groups absent
  - Admin sees admin groups with 用户管理 submenu (学员 / 教师 / 角色)
  - Unread notification badge matches `NotificationService.GetUnreadCountAsync` for a user with 1+ unread
  - Anonymous visitor at `/` does not see the sidebar
  - Non-Admin calling `/Admin/Menu` is denied
- [x] 7.3 Verify the negative scenarios: zero-count items show no badge; hiding an item in the admin editor removes it for everyone on next load; renaming a group updates the sidebar label everywhere
- [x] 7.4 Confirm roll-forward path: switching `_ViewStart` back to the previous `_Layout` reverts the shell without runtime errors