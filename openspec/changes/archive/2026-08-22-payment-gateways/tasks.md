# Payment Gateways — Tasks

## 1. Payment domain

- [x] 1.1 Add the Payments project, lifecycle models/configurations, provider interface, and module registration
- [x] 1.2 Implement at least one sandbox adapter plus a deterministic fake adapter for tests
- [x] 1.3 Implement idempotent transitions, verified webhook ingestion, fulfillment outbox, refunds, and reconciliation
- [x] 1.4 Add database registration and an EF Core migration

## 2. Workflows

- [x] 2.1 Replace direct payment confirmation with create/return/pending/status flows
- [x] 2.2 Add protected webhook endpoints with raw-body signature verification and replay defense
- [x] 2.3 Add Admin provider-health, reconciliation-exception, and refund pages with secret-safe configuration

## 3. Verification

- [x] 3.1 Test invalid signatures, duplicate/out-of-order callbacks, amount mismatch, retries, refunds, and log redaction
- [x] 3.2 Build cleanly and exercise checkout through sandbox webhook fulfillment over HTTP
