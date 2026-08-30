# Extensibility and Integration Contracts Specification

## ADDED Requirements

### Requirement: Integrations declare compatible contracts

Every external integration SHALL declare its type, contract version, capabilities, tenant scope, and enabled state before it can be invoked.

#### Scenario: Compatible integration is invoked

- **WHEN** an enabled integration declares a supported contract version and capability
- **THEN** the platform validates the request and invokes the appropriate adapter

#### Scenario: Unknown version is invoked

- **WHEN** an integration declares an unsupported contract version
- **THEN** invocation fails closed with an operator diagnostic and no external request is made

### Requirement: External delivery is idempotent and bounded

External deliveries SHALL use an idempotency key, timeout, bounded retry policy, and dead-letter disposition, and MUST NOT block unrelated learning workflows after failure.

#### Scenario: Provider times out

- **WHEN** an external provider exceeds the configured timeout
- **THEN** the attempt is recorded and retried within bounds, then dead-lettered if exhaustion occurs

#### Scenario: Delivery is duplicated

- **WHEN** the same event is submitted again with the same idempotency key
- **THEN** the platform returns the prior effective disposition without duplicating the external side effect

### Requirement: Integration operations are auditable and scoped

The system SHALL record integration attempts, outcomes, retries, and replay actions without secrets, and SHALL restrict diagnostics and replay to authorized tenant-scoped administrators.

#### Scenario: Admin replays dead letter

- **WHEN** an authorized administrator replays a dead-letter item
- **THEN** a new attempt is created with the original event identity and an audit record

#### Scenario: Unauthorized replay

- **WHEN** a non-admin or administrator outside the tenant requests replay
- **THEN** access is denied and no external delivery occurs
