# Platform Analytics & Reports — Design

## Context

Dashboards (`dashboards` change) cover at-a-glance KPIs. This change adds parameterized, exportable reports for the operator over the same underlying tables.

## Goals

- Admin can answer: revenue by course/instructor for a period, enrollments over time, signups over time.
- Admin can export CSV of orders, enrollments, and users.

## Non-Goals

- No interactive charting library (server-rendered tables + simple bars for now).
- No cohort/retention analysis.
- No audit log (documented separately).

## Decisions

### D1: On-demand SQL aggregations
Each report is a parameterized query (start/end date) against existing tables:
- Revenue: `Orders` filtered by `Paid` and `PaidAt` in range, grouped by `CourseId` (+ join to instructor).
- Enrollments: `Enrollments` grouped by day (or by course), filtered by `EnrolledAt`.
- Users: `AspNetUsers` grouped by day, plus role breakdown via `AspNetUserRoles`.

### D2: Report pages
`/Admin/Reports/Revenue`, `/Enrollments`, `/Users` — admin-only Razor Pages with a date-range form and a result table (top-N + totals). Simple CSS bars for day counts. No JS chart lib.

### D3: CSV export
Export actions return `text/csv` with a `Content-Disposition` attachment. Rows: orders (id, date, course, student, amount, status, reference), enrollments (id, date, course, student), users (id, date, email, roles, suspended). Implemented with a tiny CSV writer (escape quotes/commas) — no package.

### D4: Where queries live
Period-filtered query methods go on `OrderService` (revenue) and `EnrollmentService` (enrollments); the user report query is a method on a new `UserReportService` in the Auth module (or the existing `UserManagement` service when that lands). Decision: put user-report queries in `OpenLearning.Auth` (`UserService`) to avoid a dependency on the not-yet-existing user-management module.

## Risks / Trade-offs

- **Query cost on large tables** → Indexes exist on dates/status; reports run on demand; acceptable.
- **CSV injection** → Cell values are quoted and `=+-@` prefixed cells are neutralized.

## Migration Plan

No schema changes.

## Open Questions

- Currency/period defaults — default to all-time; configurable period via UI.
