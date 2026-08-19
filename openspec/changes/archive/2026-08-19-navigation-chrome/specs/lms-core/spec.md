## MODIFIED Requirements

### Requirement: Application provides shared navigation

The system SHALL render a layout shell consisting of a top-bar, a collapsible sidebar with grouped menu items, and a main content area for every authenticated page. The sidebar's items SHALL be filtered server-side to the user's roles so entries the user is not entitled to are never rendered.

#### Scenario: Student sees student sidebar

- **WHEN** a Student is signed in and visits a student-area page
- **THEN** the sidebar shows the student menu groups (学习中心, 我的课程, 课程目录, 作业练习, 学习资料, 讨论社区, 学习记录, 个人中心)
- **THEN** the sidebar does NOT show the instructor or admin groups

#### Scenario: Instructor sees instructor links

- **WHEN** an Instructor is signed in
- **THEN** the navigation shows links to manage their courses

#### Scenario: Admin sees admin links

- **WHEN** an Admin is signed in
- **THEN** the navigation shows links to the admin console

#### Scenario: Instructor sees instructor sidebar

- **WHEN** an Instructor is signed in and visits an instructor-area page
- **THEN** the sidebar shows the instructor menu groups (教师工作台, 课程管理, 作业考试管理, 学员管理, 问答管理, 成绩统计)
- **THEN** the sidebar does NOT show the admin groups

#### Scenario: Admin sees admin sidebar

- **WHEN** an Admin is signed in and visits an admin-area page
- **THEN** the sidebar shows the admin menu groups (后台首页, 用户管理, 课程管理, 资源管理, 考试题库管理, 系统配置)
- **THEN** the user-management submenu (学员, 教师, 角色) is grouped under "用户管理"

## ADDED Requirements

### Requirement: Sidebar groups can be collapsed and expanded

The system SHALL allow the user to collapse or expand any sidebar group and SHALL persist the collapsed/expanded state per user across sessions.

#### Scenario: Collapse a group

- **WHEN** a user collapses a sidebar group
- **THEN** the group's items are hidden and the group shows only its label and chevron

#### Scenario: Persist collapsed state

- **WHEN** a user reloads the page after collapsing a group
- **THEN** the group remains collapsed

#### Scenario: Default expansion

- **WHEN** a user has never set a preference for a group
- **THEN** the group is expanded by default

### Requirement: Sidebar shows badges for relevant counters

The system SHALL render a numeric badge next to a sidebar item when the owning module exposes a counter for the current user (e.g. notifications unread, assignments to grade, study items to complete).

#### Scenario: Unread notifications badge

- **WHEN** a signed-in user has unread notifications
- **THEN** the sidebar's Notifications item shows a badge with the unread count

#### Scenario: No badge when zero

- **WHEN** a counter value is zero
- **THEN** no badge is rendered for that item

### Requirement: Every page shows a breadcrumb

The system SHALL render a breadcrumb at the top of the main content area on every page that uses the sidebar layout. Each breadcrumb segment SHALL be a link except the last segment which represents the current page.

#### Scenario: Admin user list breadcrumb

- **WHEN** an Admin opens the user list
- **THEN** the breadcrumb shows `首页 > 后台 > 用户管理`

### Requirement: Sidebar collapses to icons on narrow viewports

The system SHALL collapse the sidebar to icon-only mode below a configured breakpoint and SHALL provide a top-bar toggle to reopen it as an overlay.

#### Scenario: Narrow viewport

- **WHEN** the viewport width is below the breakpoint
- **THEN** the sidebar is hidden by default and an icon toggle is shown in the top-bar

#### Scenario: Toggle opens overlay

- **WHEN** the user clicks the top-bar toggle on a narrow viewport
- **THEN** the sidebar opens as an overlay without pushing the main content

### Requirement: Sidebar reflects the admin-configured menu tree

The system SHALL read the menu tree from `menu-config` on each request, merge it with built-in defaults, filter by the current user's roles, and render the resulting groups and items in the sidebar.

#### Scenario: Admin hides an item

- **WHEN** an Admin marks a menu item as hidden
- **THEN** no user sees that item in the sidebar

#### Scenario: Admin reorders items

- **WHEN** an Admin changes the sort order of items in a group
- **THEN** the new order is reflected in every user's sidebar after the next page load

#### Scenario: Admin restricts an item to a role

- **WHEN** an Admin sets an item's allowed roles to `Admin`
- **THEN** only Admin users see that item; other roles do not

### Requirement: Top-bar shows search, notifications, and avatar

The system SHALL render a top-bar above the sidebar with: the site logo (left), a global search input (center, when enabled), and a notifications icon plus an avatar dropdown (right).

#### Scenario: Notifications icon mirrors unread count

- **WHEN** the user has unread notifications
- **THEN** the top-bar notifications icon shows the unread count as a badge in addition to (or as a fallback for) the sidebar badge

#### Scenario: Avatar dropdown

- **WHEN** the user clicks the avatar
- **THEN** a dropdown shows 个人设置, 修改密码, 退出登录
