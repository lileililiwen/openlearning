## ADDED Requirements

### Requirement: Instructor views revenue

The system SHALL show an Instructor their earned revenue as a total, per course, and per period, along with their available withdrawal balance.

#### Scenario: Revenue page
- **WHEN** an Instructor opens their revenue page
- **THEN** total earned, per-course breakdown, and available balance are shown

#### Scenario: Paid order credits
- **WHEN** a course order is paid
- **THEN** the instructor's ledger is credited with the order amount

### Requirement: Instructor requests withdrawals

The system SHALL allow an Instructor with sufficient available balance to request a withdrawal, and SHALL let an Admin review it.

#### Scenario: Request withdrawal
- **WHEN** an Instructor's available balance meets the minimum and they request a withdrawal
- **THEN** a pending withdrawal request is created and the balance is reserved

#### Scenario: Insufficient balance
- **WHEN** an Instructor requests a withdrawal above their available balance
- **THEN** the request is rejected

#### Scenario: Review outcome
- **WHEN** an Admin marks a withdrawal paid or rejected
- **THEN** the status changes and the Instructor is notified
