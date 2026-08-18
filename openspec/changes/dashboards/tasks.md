# Dashboards — Tasks

## 1. Data Support

- [ ] 1.1 Add `LessonAccess` entity + config in the Progress module (unique per enrollment+lesson, timestamped)
- [ ] 1.2 Record lesson access on lesson open in the lesson view page
- [ ] 1.3 Add dashboard aggregation helpers (enrollment counts, completion rate, quiz pass rate, revenue) to owning module services

## 2. Student Dashboard

- [ ] 2.1 Implement student dashboard page: enrolled courses + progress, quiz status, certificates summary, continue-learning resume
- [ ] 2.2 Add recommendations (same-category published courses, newest first)
- [ ] 2.3 Role-aware landing: redirect signed-in users from `/` to their dashboard

## 3. Teacher Dashboard

- [ ] 3.1 Implement teacher dashboard page with per-course stats and quick links

## 4. Platform Dashboard

- [ ] 4.1 Implement admin dashboard page with platform KPIs, recent activity, and management links

## 5. Migration & Verification

- [ ] 5.1 Create EF Core migration for `LessonAccess`
- [ ] 5.2 Run `dotnet build` and start the app
- [ ] 5.3 Verify dashboards render for each role and resume deep-link works
