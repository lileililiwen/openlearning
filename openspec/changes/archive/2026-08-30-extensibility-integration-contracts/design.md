# Design: Extensibility and Integration Contracts

## Decisions

- Define an integration registration with name, type, contract version, capabilities, tenant scope, endpoint/configuration reference, and enabled state.
- Adapters expose validate, execute, and health semantics behind a common boundary; protocol modules remain responsible for protocol details.
- Outbound deliveries carry an idempotency key, bounded retry policy, timeout, and dead-letter state. Retries never bypass authorization or consent checks.
- Record launch/delivery attempts, response class, latency, retry count, and final disposition without secrets or sensitive payloads.
- Admins can disable an integration and replay a safe dead-letter item; replay preserves the original event identity and audit trail.

## Failure and compatibility

Unknown contract versions fail closed with an actionable diagnostic. A provider timeout does not block unrelated learning workflows. Schema changes require backward-compatible version negotiation.

## Verification

Test adapter isolation, version mismatch, duplicate delivery, timeout/retry exhaustion, disable/replay, tenant boundaries, and secret redaction.
