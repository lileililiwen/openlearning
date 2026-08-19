# commerce-extras Specification

## Purpose
TBD - created by archiving change commerce-extras. Update Purpose after archive.
## Requirements
### Requirement: Student can use a shopping cart

The system SHALL allow a Student to add courses to a cart, remove them, and check out one or more items together.

#### Scenario: Add to cart
- **WHEN** a Student adds a published course to their cart
- **THEN** the course appears in their cart without being ordered

#### Scenario: Checkout multiple items
- **WHEN** a Student checks out their cart
- **THEN** an order is created for each course and payment completes them

#### Scenario: Remove item
- **WHEN** a Student removes a course from the cart
- **THEN** the course no longer appears in the cart

### Requirement: Coupons, balance, and points apply at checkout

The system SHALL let a Student apply a valid coupon and use account balance or loyalty points to reduce an order's cost.

#### Scenario: Apply coupon
- **WHEN** a Student applies a valid, unused coupon at checkout
- **THEN** the discount is applied and the coupon use is recorded

#### Scenario: Invalid coupon
- **WHEN** a Student applies an expired, unknown, or exhausted coupon
- **THEN** the coupon is rejected with a message

#### Scenario: Balance/points payment
- **WHEN** a Student pays with balance or points
- **THEN** ledger entries are recorded and the order amount reflects the reduction

### Requirement: Student can request refunds and invoices

The system SHALL allow a Student to request a refund on a paid order and to request an invoice, recording both for later review.

#### Scenario: Request refund
- **WHEN** a Student requests a refund on a paid order
- **THEN** the order is marked as refund requested and the Admin is notified

#### Scenario: Request invoice
- **WHEN** a Student requests an invoice on a paid order
- **THEN** the request is stored for the order

