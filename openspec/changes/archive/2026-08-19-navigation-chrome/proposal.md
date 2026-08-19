## Why

The current shell is a single Bootstrap top-bar (`Pages/Shared/_Layout.cshtml`) with flat, role-conditional lists. It does not scale to the role-aware menu structure the product brief requires (sidebar + topbar, grouped collapsible items, status badges, breadcrumbs) and gives admins no way to manage the menu itself. We need a layout shell that renders the right sidebar group per role, supports badge counts for navigation items, exposes breadcrumbs on every page, and lets an Admin configure menu structure under the existing `system-config` model — without rewriting individual pages.

## What Changes

- Introduce a `Sidebar + Topbar + Main` layout shell used by all role-area pages (student / instructor / admin), with a collapsible grouped sidebar whose items, group labels, and order come from a server-rendered menu model.
- Add a `Breadcrumb` partial + `IBreadcrumbProvider` so every page contributes ancestors and the layout renders `首页 > 我的课程 > 课程` automatically.
- Add a `NavCounter` hook so navigation items can display counts (通知 unread already exists; the shell additionally surfaces 待批改 for teachers and 待完成 for students where the owning module exposes them).
- Allow the Admin to manage the menu tree under `Admin / System / Menu` (group label, item label, route, allowed roles, sort order, hidden). The menu is evaluated server-side per request, so users never see entries they are not entitled to.
- Replace the existing `lms-core` "shared navigation" requirement with the new shell, and update `notifications` so the unread count renders inside the sidebar instead of the top-bar bell (preserve the count semantic; the topbar bell becomes optional fallback).

## Capabilities

### New Capabilities

- `navigation-chrome`: sidebar + topbar layout, role-rendered grouped menus, collapse/expand, breadcrumb provider, and responsive (mobile) collapse rules.
- `menu-config`: admin-managed menu tree (groups, items, roles allowed, sort order, hidden) persisted via the `system-config` storage pattern; the menu is evaluated server-side and merged with built-in defaults.

### Modified Capabilities

- `lms-core`: replace "Application provides shared navigation" with a sidebar+topbar+breadcrumb requirement; the existing seed-data and license requirements stay.
- `notifications`: the unread-count badge moves from the top-bar bell to the sidebar's Notifications item, with a fallback bell on small viewports; the "Unread badge" scenario is restated to reflect the new placement.

## Impact

- New module `OpenLearning.Navigation`: `MenuItem { Id, GroupKey, Key, Label, Route, AllowedRoles, SortOrder, Hidden, IconKey? }`, `MenuGroup { Key, Label, SortOrder }`; `MenuService` (server-rendered tree, role-filtered, cache per request) and `BreadcrumbService` (collects ancestors from a route-data convention).
- Razor: new `_SidebarLayout.cshtml` and `_Topbar.cshtml` partials; `_Layout.cshtml` becomes a thin wrapper that selects `_SidebarLayout` or, for the future course player, a focused layout. A `_Breadcrumbs.cshtml` partial is included by the sidebar layout.
- Admin pages: `Pages/Admin/Menu/Index.cshtml` (list/reorder groups and items, toggle hidden, edit allowed roles); new policy `AdminMenuConfig`.
- CSS: introduce a `site-chrome.css` partial with sidebar/topbar/breadcrumb styles; the existing `site.css` is reused for buttons, cards, tables.
- No change to `OpenLearning.Data` schema for the menu tree — values are stored via `system-config`'s JSON value model (key `navigation.menu.v1`), so no EF migration is needed for `menu-config` itself. `navigation-chrome` introduces no new tables.
- The change is additive; existing pages continue to render inside the new shell without per-page rewrites. The course-player-focused layout (sidebar hidden) is a separate change.