## Context

The current shell is a single Bootstrap top-bar (`Pages/Shared/_Layout.cshtml`) with a flat list of role-conditional `<li>` elements. It has no sidebar, no breadcrumb, no grouped menu, and no way for admins to manage the menu itself. The brief requires a sidebar + topbar layout with grouped collapsible menus, badges, and breadcrumbs, plus admin-configurable menu structure under the existing `system-config` storage.

We will keep the change additive: introduce a new `OpenLearning.Navigation` module, replace `_Layout.cshtml` with a thin wrapper that selects the sidebar shell, and add the admin menu-management page. Per Agents.md §2.1, `Navigation` follows the fixed domain pattern: class library, services over the base `DbContext`, one-line DI registration, no module → `OpenLearning.Data` reference. Menu data is stored as JSON under the existing `system-config` key model, so no EF migration is required.

## Goals / Non-Goals

**Goals:**

- A sidebar + topbar shell that renders the right group per role, supports collapse/expand (persisted per user), badge counts, and breadcrumbs on every page.
- An admin page that lets the operator rename/reorder/hide/restrict menu groups and items, persisted under `system-config`.
- Reuse the existing `NotificationService.GetUnreadCountAsync` for the notifications badge; expose a small `INavCounterProvider` so other modules can contribute counts without the navigation module referencing them.
- Responsive: collapse to icon-only below the configured breakpoint with a top-bar toggle.

**Non-Goals:**

- The focused course-player layout (sidebar hidden for video lessons) — handled by the separate `course-player-layout` change.
- Per-user theme/accent color, drag-and-drop menu editing on the front-end, or multi-tenant menu trees — out of scope.
- Rewriting existing pages' content; the shell replaces the wrapper only.

## Decisions

- **Store menu as JSON in `system-config`** instead of a new EF table. Reasons: (a) no migration, (b) consistent with how `Site.Name`, catalog page size, etc. are stored, (c) the menu is small and rarely written. Alternative considered: a new `OpenLearning.Navigation` table — rejected because it adds a migration and a DbContext registration without buying anything the JSON store doesn't already provide.
- **Server-rendered menu tree per request**, not a client-side fetch. Reasons: (a) eliminates a round-trip on first paint, (b) makes role filtering impossible to bypass client-side, (c) aligns with the existing Razor Pages model. Cache is request-scoped (services.AddScoped).
- **`INavCounterProvider` registry** for badges. Each module registers a provider keyed by menu item key (e.g. `assignments.pending-grading`, `study.todo`). The navigation service aggregates and returns `{ key, count }` pairs; the sidebar partial renders them. Alternative considered: inline counter logic in the sidebar — rejected because it would couple navigation to every owning module.
- **`BreadcrumbAttribute` on page-model classes** rather than a separate XML/YAML registry. Reasons: existing pages already use Razor Page conventions; an attribute is discoverable in one place and trivial to add per page. Alternative considered: route-data only — too implicit for deep hierarchies.
- **Sidebar groups persisted via `HttpContext` cookies** (signed) under `nav.collapsed` rather than a DB column. Reasons: lightweight, anonymous users get a default, no schema change. Alternative considered: a `NavPreferences` table — rejected as overkill for a small JSON-serialised set.
- **Replace `_Layout.cshtml` with two layouts**: `_SidebarLayout.cshtml` (default) and a thin `_Layout.cshtml` that delegates. Pages do not change their layout reference; the change is invisible to existing pages.

## Risks / Trade-offs

- [Risk: Menu JSON becomes hard to edit by hand as the app grows] → Mitigation: the admin page renders a structured editor (groups, items table) so the JSON is never hand-edited in production.
- [Risk: Badges from many modules add latency to every page] → Mitigation: each `INavCounterProvider` is registered with an optional `TimeSpan` cache; the navigation service aggregates in parallel and short-circuits on empty keys.
- [Risk: Sidebar layout breaks narrow viewports if CSS isn't responsive-tested] → Mitigation: the design ships with a `site-chrome.css` partial and a manual smoke-test on `<768px`, `<1024px`, and `>=1280px` for each role.
- [Risk: Replacing `_Layout.cshtml` touches every page indirectly] → Mitigation: pages opt into the layout via `_ViewStart.cshtml`; we change `_ViewStart` to point at the new wrapper, not each page.
- [Risk: Existing top-bar dependencies (notification bell, cart count, profile link) regress] → Mitigation: these move to the new top-bar with their existing styling; the change covers the negative scenarios (signed-out visitor, signed-in student with zero notifications, signed-in admin).

## Migration Plan

1. Land the navigation module (`OpenLearning.Navigation`) and admin menu page behind the new `_SidebarLayout` while the existing `_Layout` still functions — implemented by switching `_ViewStart` to point at the new wrapper in the same commit.
2. Verify per role: each role sees only its groups, breadcrumbs render on every page, the unread badge is correct, the admin can hide/reorder items.
3. Smoke-test responsive collapse at 360px, 768px, and 1280px.
4. Rollback: revert `_ViewStart.cshtml` to the previous `_Layout` and remove the sidebar shell; the navigation module is dormant.

## Open Questions

- Should sidebar collapse state sync across devices for the same user, or stay per-browser? Current decision: per-browser (cookie). Revisit if user feedback indicates otherwise.
- Should we expose menu export/import (JSON file) for ops? Out of scope here; `system-config` already has a generic value-editor UI to copy/paste the value if needed.