## Why

Every actor currently lands on a generic page after sign-in: students see the catalog, instructors see a course list, and admins see a flat course table. None of them can see "what needs my attention next". Dashboards give each role an at-a-glance landing that drives the next action and links into the management surfaces they will need.

## What Changes

- **Student dashboard** (`/dashboard`): continue-learning resume, enrolled courses with progress, quiz/certificate summary, recent activity, and recommended courses.
- **Teacher dashboard**: per-course aggregates (enrollments, revenue, completion rate, quiz pass rate), recent activity, and quick links into course management.
- **Platform (admin) dashboard**: platform KPIs (users by role, courses, enrollments, revenue, completion rate, recent signups/courses) and links into user/course management.
- Dashboards are read surfaces over existing data plus small query helpers; no new persistence except optional materialized stats (deferred).

## Capabilities

### New Capabilities
- `student-dashboard`: A personalized student landing page with resume, progress, quiz/certificate status, and recommendations.
- `teacher-dashboard`: An instructor landing page with per-course teaching statistics and quick actions.
- `platform-dashboard`: An admin landing page with platform KPIs and operational links.

### Modified Capabilities

None.

## Impact

- New Razor Pages under `Pages/Dashboard/` (student/teacher) and `Pages/Admin/` (platform), with a role-aware redirect from `/` for signed-in users.
- Small aggregation queries added to existing services (enrollment counts, completion rates, quiz pass rates) in their owning modules.
- No schema changes for the MVP of this change; future analytics can add snapshots.
