# Analytics Expansion — Tasks

## 1. Events and aggregation

- [x] 1.1 Add versioned event, aggregate, refresh-run, retention, and export-audit models
- [x] 1.2 Implement allowlisted event ingestion with deduplication and pseudonymous actor keys
- [x] 1.3 Implement atomic scheduled aggregates for funnels, engagement, cohorts, assessments, and teaching workload
- [x] 1.4 Add database registration and an EF Core migration

## 2. Reports

- [x] 2.1 Add Admin learning analytics dashboards with date/course/cohort filters and freshness indicators
- [x] 2.2 Extend instructor reporting with owned-course engagement, assessment, live/teaching hours, and workload data
- [x] 2.3 Add authorized CSV exports, small-cohort suppression, and retention controls

## 3. Verification

- [x] 3.1 Test duplicate/late events, atomic refresh, tenant/course scope, suppression, and export auditing
- [x] 3.2 Build cleanly and exercise every report and denial scenario over HTTP
