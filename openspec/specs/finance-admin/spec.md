# finance-admin Specification

## Purpose
TBD - created by archiving change finance-admin. Update Purpose after archive.
## Requirements
### Requirement: Admin views and filters all orders

The system SHALL allow an Admin to list every order with filters (status, date, course, student) and totals.

#### Scenario: Order list
- **WHEN** an Admin opens the orders page
- **THEN** all orders are shown with filters and summary totals

### Requirement: Admin reviews refunds

The system SHALL allow an Admin to approve or reject refund requests and SHALL reverse the instructor's earned amount on approval.

#### Scenario: Approve refund
- **WHEN** an Admin approves a refund request
- **THEN** the order is marked refunded, the instructor's ledger is debited, and the student is notified

#### Scenario: Reject refund
- **WHEN** an Admin rejects a refund request
- **THEN** the order stays paid and the student is notified

### Requirement: Admin manages coupons

The system SHALL allow an Admin to create, edit, and deactivate discount coupons.

#### Scenario: Coupon CRUD
- **WHEN** an Admin creates or edits a coupon
- **THEN** the coupon is available for students to apply within its limits

#### Scenario: Deactivate coupon
- **WHEN** an Admin deactivates a coupon
- **THEN** the coupon can no longer be applied

### Requirement: Admin reconciles revenue

The system SHALL show an Admin a reconciliation of gross orders, refunds, and net revenue over a period, with CSV export.

#### Scenario: Reconciliation
- **WHEN** an Admin selects a period
- **THEN** gross, refunds, and net are shown per course and in total, and can be exported as CSV

