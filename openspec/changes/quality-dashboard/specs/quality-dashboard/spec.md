## ADDED Requirements

### Requirement: Quality state is aggregated in one place

The system SHALL aggregate quality metrics (build status, coverage, analyzer/sonar findings, vulnerability audit) into a generated dashboard.

#### Scenario: Dashboard reflects the latest run
- **WHEN** a CI run completes
- **THEN** its build, coverage, sonar, and audit metrics are reflected in the dashboard

#### Scenario: Missing metric
- **WHEN** one metric source is unavailable
- **THEN** the dashboard shows a default/unknown value without failing

### Requirement: Periodic quality reports show trends

The system SHALL produce a scheduled quality report summarizing the latest metrics and their trend over recent runs.

#### Scenario: Scheduled report
- **WHEN** the scheduled quality job runs
- **THEN** a report with the current values and a trend table is posted to the repository

#### Scenario: Regression visible
- **WHEN** a metric regresses (e.g. coverage drops or a new vulnerability appears)
- **THEN** the regression is highlighted in the report
