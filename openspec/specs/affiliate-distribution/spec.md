# affiliate-distribution Specification

## Purpose
TBD - created by archiving change affiliate-distribution. Update Purpose after archive.
## Requirements
### Requirement: Distributor role exists

The system SHALL define a `Distributor` role and SHALL add the policy `RequireDistributor` that gates distributor-only pages.

#### Scenario: Role seeded

- **WHEN** the application starts with an empty database
- **THEN** `Distributor` is seeded alongside the other roles

#### Scenario: Distributor-only page denied to student

- **WHEN** a Student opens `/Distributor/Index`
- **THEN** access is denied with a 403/redirect

### Requirement: Distributor creates share links per course

The system SHALL allow a Distributor to create a share link for any published course and SHALL expose a public redirect endpoint `/D/C/{slug}` that records a click and forwards the visitor to the course's details page.

#### Scenario: Create link

- **WHEN** a Distributor requests a share link for a published course
- **THEN** an `AffiliateLink` row is created with a unique slug and the URL `/D/C/{slug}` is returned

#### Scenario: Click is recorded

- **WHEN** a visitor opens `/D/C/{slug}`
- **THEN** an `AffiliateClick` is recorded with hashed IP, user agent, and an anonymous id stored in a first-party cookie
- **THEN** the visitor is redirected to the course details page with the anonymous id preserved

#### Scenario: Non-existent slug returns 404

- **WHEN** a visitor opens `/D/C/{unknown}`
- **THEN** the request 404s without recording a click

### Requirement: Orders are attributed to a distributor

The system SHALL attribute a paid order to a Distributor when an `AffiliateClick` for the same course was recorded for the same anonymous id within the previous 30 days, and SHALL create a corresponding `CommissionEntry`.

#### Scenario: Paid order is attributed

- **WHEN** an order is paid and an `AffiliateClick` matches within the 30-day window
- **THEN** an `Attribution` and a `CommissionEntry` with `Status = Pending` are created for the distributor

#### Scenario: No click → no commission

- **WHEN** an order is paid and no matching click exists
- **THEN** no `CommissionEntry` is created

#### Scenario: Expired window

- **WHEN** an order is paid and the most recent click for that id is older than 30 days
- **THEN** no attribution is created

### Requirement: Commission is reserved on refund

The system SHALL mark a `CommissionEntry` as `Reversed` when the underlying order is refunded and SHALL exclude reversed entries from the distributor's available balance.

#### Scenario: Refund reverses commission

- **WHEN** an Admin/Finance approves a refund for an attributed order
- **THEN** the corresponding `CommissionEntry` is set to `Status = Reversed` and the distributor's available balance decreases

#### Scenario: Reversal after payout

- **WHEN** a refund is approved after the commission has already been paid out
- **THEN** the reversal is recorded as a negative `CommissionEntry` (clawback) for the next payout cycle

### Requirement: Distributor can request a payout

The system SHALL allow a Distributor with sufficient available balance to request a payout; the request SHALL be visible to Admin/Finance for review.

#### Scenario: Request payout

- **WHEN** a Distributor requests a payout of an amount ≤ their available balance
- **THEN** a `PayoutRequest` is created with `Status = Pending` and the balance is reserved

#### Scenario: Insufficient balance

- **WHEN** a Distributor requests a payout above their available balance
- **THEN** the request is rejected

#### Scenario: Admin approves

- **WHEN** an Admin (or Finance) approves a pending payout
- **THEN** the request becomes `Approved` and the corresponding `CommissionEntry` rows become `Paid`

#### Scenario: Admin rejects

- **WHEN** an Admin (or Finance) rejects a pending payout
- **THEN** the request becomes `Rejected` and the reserved balance is returned to the distributor

### Requirement: Periodic settlement closes a period

The system SHALL run a periodic settlement job (cron provided by `job-scheduler`) that freezes all `Pending` commissions into `Available` after a configurable holding period, and emits a settlement statement per distributor for the period.

#### Scenario: Holding period

- **WHEN** a `CommissionEntry` reaches the configured holding period (default 7 days after order paid)
- **THEN** its status transitions from `Pending` to `Available`

#### Scenario: Period close

- **WHEN** the settlement job runs for a period (default weekly)
- **THEN** a `SettlementStatement { Id, DistributorUserId, PeriodStart, PeriodEnd, TotalAmount, Status }` is created and is immutable once closed

#### Scenario: Idempotent close

- **WHEN** the settlement job runs twice for the same period
- **THEN** the second run is skipped (job-scheduler's IdempotencyKey covers this)

