## Why

Current analytics cover revenue, signups, and enrollments, while the teacher dashboard exposes only headline course statistics. Completion funnels, learner engagement, assessment quality, and teaching workload remain absent.

## What Changes

- Add privacy-bounded learning-event collection and retention.
- Add course completion/funnel, engagement, cohort, and assessment-quality reports.
- Add instructor teaching-workload and scheduled-hours reports.
- Add export controls, small-cohort suppression, and freshness indicators.

## Capabilities

### New Capabilities
- `learning-analytics`: learning event model, aggregate reports, privacy controls, and workload metrics.

### Modified Capabilities
- None.

## Impact

- Extend `OpenLearning.Operations`/analytics projections and reuse existing platform and teacher dashboards.
- No raw event access is granted to instructors.
