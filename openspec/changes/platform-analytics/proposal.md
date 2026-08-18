## Why

The platform operator has no visibility into revenue, enrollments, or user growth beyond what the dashboard shows, and no way to export data. Reports are required for operating decisions and business continuity.

## What Changes

- **Revenue report**: paid orders grouped by course and instructor over a selected period.
- **Enrollment report**: enrollments over time, by course.
- **User report**: signups over time by role.
- **CSV export** for orders, enrollments, and users.
- Reports are admin-only, built on existing order/enrollment/user tables (no new event pipeline).

## Capabilities

### New Capabilities
- `platform-analytics`: admin reporting (revenue, enrollments, users) with period filtering and CSV export.

### Modified Capabilities

None.

## Impact

- New `Pages/Admin/Reports/` pages (revenue, enrollments, users) and export actions.
- Query helpers added to `OrderService`, `EnrollmentService`, and a user-report query (Auth module) with period filters.
- No schema changes; CSV is generated server-side with `System.Text.Json`/CSV formatting (no external package).
