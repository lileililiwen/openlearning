## ADDED Requirements

### Requirement: Checkout creates a provider-neutral payment intent

The system SHALL create one currency/amount-bound payment intent for an eligible order, select an enabled provider through configuration, and avoid storing payment-instrument data.

#### Scenario: Amount changes after intent creation
- **WHEN** the payable order amount no longer matches the intent
- **THEN** the stale intent cannot fulfill the order and a new checkout is required

### Requirement: Only verified callbacks confirm payment

The system SHALL verify webhook signatures against the raw request, enforce provider/account/amount/currency matching, deduplicate provider event IDs, and fulfill an order idempotently only after a valid success transition.

#### Scenario: Browser return claims success
- **WHEN** a user returns from a provider before a verified success callback
- **THEN** the order remains pending and no enrollment is granted

#### Scenario: Duplicate success webhook
- **WHEN** a provider retries a valid success event
- **THEN** one payment transition and one fulfillment occur

### Requirement: Refunds follow an auditable asynchronous lifecycle

The system SHALL allow authorized staff to request a bounded refund, track provider state, prevent cumulative over-refunds, and apply downstream effects only after confirmation.

#### Scenario: Refund exceeds remaining paid amount
- **WHEN** staff request a refund above the unrefunded amount
- **THEN** the request is rejected before contacting the provider

### Requirement: Payment exceptions are reconciled safely

The system SHALL reconcile eligible nonterminal payments, surface mismatches for authorized review, and never silently downgrade a confirmed payment.

#### Scenario: Provider lookup is unavailable
- **WHEN** reconciliation cannot reach a provider
- **THEN** local state remains unchanged and a retryable failure is recorded

### Requirement: Payment secrets and evidence are protected

The system SHALL keep credentials outside source/database payloads, redact sensitive values from logs, audit privileged actions, and retain only necessary provider evidence.

#### Scenario: Webhook verification fails
- **WHEN** a callback signature is invalid
- **THEN** it is rejected, safely audited, and its secret or full sensitive payload is not logged
