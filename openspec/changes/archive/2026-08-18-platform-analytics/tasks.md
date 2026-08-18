# Platform Analytics & Reports — Tasks

## 1. Query Support

- [x] 1.1 Add period-filtered revenue query to `OrderService` (grouped by course/instructor, totals)
- [x] 1.2 Add period-filtered enrollment query to `EnrollmentService` (over time / by course)
- [x] 1.3 Add `UserService` (Auth) with period-filtered signup + role breakdown query

## 2. Report UI

- [x] 2.1 `/Admin/Reports/Revenue` page (date range, table, totals)
- [x] 2.2 `/Admin/Reports/Enrollments` page (counts over time)
- [x] 2.3 `/Admin/Reports/Users` page (signups over time + role breakdown)
- [x] 2.4 Admin nav links to reports

## 3. CSV Export

- [x] 3.1 CSV writer helper (quoting + formula-neutralization) and export actions for orders, enrollments, users

## 4. Verification

- [x] 4.1 Run `dotnet build` and start the app
- [x] 4.2 Verify each report renders with filters and exports correct CSV (admin only; non-admin denied)
