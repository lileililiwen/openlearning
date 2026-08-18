## Why

Instructors can see per-course orders and a dashboard KPI, but the reference system's Instructor side requires a Revenue Settlement module: revenue viewing (per course and over time) and withdrawal requests.

## What Changes

- Instructor revenue page: total earned, per-course breakdown, per-period totals, and pending settlement balance.
- Withdrawal requests: an instructor requests a payout of their available balance; admin reviews and marks it paid (`finance-admin` handles the admin side).
- Settlement ledger tracks earned amounts, pending withdrawals, and paid withdrawals per instructor.

## Capabilities

### New Capabilities
- `instructor-revenue`: instructor revenue views and withdrawal requests.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Settlement` module: `SettlementLedger { Id, InstructorId, CourseId?, Amount, Reason, CreatedAt }`, `WithdrawalRequest { Id, InstructorId, Amount, Status, RequestedAt, ReviewedAt, ReviewedBy }`.
- `SettlementService` (ledger add on paid order, balance query, request withdrawal, list requests).
- Instructor page `/Instructor/Revenue`; payout eligibility (minimum balance) documented.
