# Implementation Tasks

## 1. Contract model

- [x] Define integration registration, capability/version metadata, delivery attempt, dead-letter, and audit entities/configurations.
- [x] Define adapter interfaces and protocol-neutral result/error categories.

## 2. Delivery runtime

- [x] Implement validation, timeout, bounded retry, idempotency, dead-letter, and disable behavior (self-contained runtime over the base DbContext).
- [x] Add unit tests for version mismatch, duplicate events, timeout exhaustion, and secret redaction.

## 3. Protocol integration and operations

- [x] Adapt LTI, SCORM, and webhook entry points to the contract without duplicating protocol logic (shared `HttpIntegrationAdapterBase` + Webhook/Lti/Scorm adapters).
- [x] Add admin diagnostics, safe replay, authorization, tenant scoping, and PostgreSQL integration tests.

## 4. Verification

- [x] Run integration tests, build, format check, migration verification, and OpenSpec validation.
