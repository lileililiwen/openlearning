## ADDED Requirements

### Requirement: Sidebar renders grouped menu items per role

The system SHALL provide a sidebar component that renders grouped menu items filtered to the signed-in user's roles. Each group has a label, an icon (optional), and a sortable list of items; each item has a label, a route, an icon (optional), and an optional counter.

#### Scenario: Student sidebar groups

- **WHEN** a Student signs in
- **THEN** the sidebar groups are rendered in this order: 学习中心, 我的课程, 课程目录, 作业练习, 学习资料, 讨论社区, 学习记录, 个人中心

#### Scenario: Instructor sidebar groups

- **WHEN** an Instructor signs in
- **THEN** the sidebar groups are rendered in this order: 教师工作台, 课程管理, 作业考试管理, 学员管理, 问答管理, 成绩统计

#### Scenario: Admin sidebar groups

- **WHEN** an Admin signs in
- **THEN** the sidebar groups are rendered in this order: 后台首页, 用户管理, 课程管理, 资源管理, 考试题库管理, 系统配置

#### Scenario: Anonymous visitor sees no sidebar

- **WHEN** no user is signed in
- **THEN** the sidebar is not rendered (the top-bar shows Sign in / Register)

### Requirement: Top-bar renders site-wide actions

The system SHALL render a top-bar with the site logo, an optional global search input, and a right-aligned notifications icon and avatar dropdown.

#### Scenario: Logo links home

- **WHEN** a signed-in user clicks the site logo
- **THEN** they are taken to their role's dashboard

#### Scenario: Avatar dropdown actions

- **WHEN** the user opens the avatar dropdown
- **THEN** the entries shown are 个人设置, 修改密码, 退出登录

### Requirement: Breadcrumbs reflect the page hierarchy

The system SHALL render a breadcrumb at the top of the main content area built from page-level metadata. Pages contribute ancestors via a `BreadcrumbAttribute` (route-data) or an `IBreadcrumbProvider` registered per page.

#### Scenario: Deep breadcrumb

- **WHEN** a page declares ancestors `home, my-courses, course-detail, lesson-detail`
- **THEN** the breadcrumb renders four links separated by a chevron

#### Scenario: Current segment not a link

- **WHEN** the breadcrumb has more than one segment
- **THEN** the last segment is rendered as plain text (not a link)

### Requirement: Sidebar collapse state persists per user

The system SHALL persist the collapsed/expanded state of each sidebar group per user so it survives reloads and sessions.

#### Scenario: Persist collapse

- **WHEN** a user collapses a group
- **THEN** the next page load shows the group in the collapsed state

### Requirement: Responsive sidebar collapse on narrow viewports

The system SHALL collapse the sidebar to icon-only mode below a configured breakpoint and SHALL provide a top-bar toggle to open it as an overlay.

#### Scenario: Narrow viewport hides labels

- **WHEN** the viewport width is below the breakpoint
- **THEN** only group and item icons are shown

#### Scenario: Top-bar toggle reopens overlay

- **WHEN** the user clicks the top-bar toggle on a narrow viewport
- **THEN** the sidebar slides in as an overlay