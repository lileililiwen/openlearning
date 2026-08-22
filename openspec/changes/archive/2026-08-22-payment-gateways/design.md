# Payment Gateways — Design

## Context

Payment providers are asynchronous and retry callbacks. Order fulfillment must not trust browser redirects or duplicate events.

## Goals

- Isolate provider SDKs behind a stable application interface.
- Implement idempotent payment/refund state transitions.
- Verify, retain, and reconcile provider evidence securely.

## Non-Goals

- Holding card data, acting as a wallet, or replacing financial settlement.
- Multi-currency conversion.

## Decisions

### Existing components reused

The implementation reuses Ecommerce `Order` as the payable amount and fulfillment target, `EnrollmentService` for idempotent course access, the existing Finance/Admin authorization policy for privileged workflows, and the Razor Pages plus central `ApplicationDbContext` composition patterns. It replaces the browser-driven `OrderService.ConfirmPaymentAsync` checkout call without duplicating order or enrollment aggregates.

### D1: Payment aggregate and adapter boundary

`PaymentIntent`, `PaymentAttempt`, `Refund`, and `ProviderEvent` live in Payments. Adapters create sessions, verify webhooks, query status, and request refunds.

### D2: Webhook-authoritative fulfillment

Successful verified provider events transition payment state and invoke idempotent order fulfillment. Return URLs display status but never confirm payment.

### D3: Secret and payload handling

Secrets come from protected configuration. Store provider IDs, hashes, parsed allowlisted fields, and redacted diagnostic payloads; never log credentials or payment instruments.

### D4: Reconciliation

A scheduled job compares nonterminal/local-recent payments with provider state and raises exceptions rather than silently rewriting settled records.

## Risks / Trade-offs

- Provider outages delay fulfillment; explicit pending state and retries make this visible.
- Adapter differences constrain the common interface; provider-specific metadata stays isolated.

## Migration Plan

Add payment tables and provider configuration. Existing paid orders are marked legacy and are not replayed through a gateway.
