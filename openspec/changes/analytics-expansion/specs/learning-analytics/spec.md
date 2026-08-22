## ADDED Requirements

### Requirement: Learning events are governed and deduplicated

The system SHALL accept only versioned allowlisted learning events, minimize actor data, deduplicate repeated event identifiers, and retain events according to configured policy.

#### Scenario: Duplicate event
- **WHEN** the same event identifier is received more than once
- **THEN** it contributes exactly once to analytics

#### Scenario: Unknown property
- **WHEN** an event contains a property outside its registered schema
- **THEN** the property is rejected or discarded and the validation outcome is observable to operators

### Requirement: Operators can analyze learning outcomes

The system SHALL provide authorized Admin reports for course funnels, completion rates, active learning time, cohort retention, and assessment-item performance over selectable periods.

#### Scenario: Completion funnel
- **WHEN** an Admin selects a course and date range
- **THEN** the report shows eligible, enrolled, started, and completed counts with defined denominators

### Requirement: Instructors see scoped teaching analytics

The system SHALL show an Instructor engagement, completion, assessment-quality, scheduled teaching-hour, and grading-workload metrics only for courses they own or are authorized to teach.

#### Scenario: Non-owner course filter
- **WHEN** an Instructor submits another instructor's course identifier
- **THEN** the system SHALL deny access without returning its metrics

### Requirement: Analytics disclose freshness and protect small cohorts

The system SHALL show the last successful aggregate refresh, avoid serving partial refreshes, and suppress segmented results below a configured cohort threshold.

#### Scenario: Small cohort
- **WHEN** a filtered segment is below the privacy threshold
- **THEN** its metric is suppressed rather than exposing a count or export row

### Requirement: Analytics exports are controlled

The system SHALL apply the same authorization and suppression rules to exports and SHALL audit the requester, scope, filters, and timestamp.

#### Scenario: Export report
- **WHEN** an authorized user exports an analytics report
- **THEN** the exported values match the visible authorized scope and an audit record is created
