## Why

Ecommerce records payment confirmation but has no isolated gateway boundary, webhook verification, asynchronous state machine, reconciliation, or refund orchestration.

## What Changes

- Add provider-neutral payment intents, attempts, refunds, webhooks, and reconciliation.
- Require verified idempotent provider callbacks before fulfillment.
- Add operator configuration and exception workflows without storing card data.

## Capabilities

### New Capabilities
- `payment-gateways`: provider adapters, payment/refund lifecycle, webhooks, and reconciliation.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Payments` domain integrated with Ecommerce orders and existing settlement/invoicing services.
