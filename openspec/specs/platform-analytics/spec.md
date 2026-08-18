# platform-analytics Specification

## Purpose
TBD - created by archiving change platform-analytics. Update Purpose after archive.
## Requirements
### Requirement: Admin can view revenue reports

The system SHALL allow an Admin to view paid revenue grouped by course and instructor for a selected date range.

#### Scenario: Revenue by course
- **WHEN** an Admin selects a date range on the revenue report
- **THEN** paid revenue per course (and instructor) within the range is shown with a total

### Requirement: Admin can view enrollment and user reports

The system SHALL allow an Admin to view enrollments over time and new signups over time for a selected date range.

#### Scenario: Enrollments report
- **WHEN** an Admin selects a date range on the enrollments report
- **THEN** enrollment counts over time are shown

#### Scenario: Users report
- **WHEN** an Admin selects a date range on the users report
- **THEN** signup counts over time (with role breakdown) are shown

### Requirement: Admin can export CSV

The system SHALL allow an Admin to export orders, enrollments, and users as CSV files.

#### Scenario: Export orders
- **WHEN** an Admin clicks export on the orders report
- **THEN** a CSV file of the filtered orders is downloaded

#### Scenario: Export users
- **WHEN** an Admin clicks export on the users report
- **THEN** a CSV file of the filtered users is downloaded

