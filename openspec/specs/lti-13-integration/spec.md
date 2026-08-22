# lti-13-integration Specification

## Purpose
TBD - created by archiving change lti-13-integration. Update Purpose after archive.
## Requirements
### Requirement: Administrators control LTI registrations and deployments

The system SHALL allow an Admin to configure and revoke an LTI 1.3 platform registration, its trusted endpoints/keys, enabled deployment identifiers, capabilities, and explicit course-context mappings.

#### Scenario: Deployment is disabled
- **WHEN** a launch references a disabled or unknown deployment identifier
- **THEN** the launch is rejected before user or course access is created

### Requirement: Launches are cryptographically validated and replay-safe

The system SHALL validate OIDC state, one-time nonce, signature, issuer, audience, deployment, timestamps, LTI version, and message type before accepting a launch.

#### Scenario: Launch token replay
- **WHEN** a previously consumed nonce is presented again
- **THEN** the launch is rejected and safely audited

#### Scenario: Key rotation
- **WHEN** the platform rotates a signing key
- **THEN** the system refreshes trusted JWKS according to policy and still fails closed for an untrusted signature

### Requirement: LTI identities and roles remain scoped

The system SHALL map subjects within their registration/deployment, SHALL not auto-link accounts solely by email, and SHALL translate LTI roles only into permissions for the mapped context.

#### Scenario: Instructor role launch
- **WHEN** an LTI Instructor launches a mapped course
- **THEN** any granted teaching permission is limited to that mapped course and deployment

### Requirement: LTI Advantage services use explicit least privilege

The system SHALL enable Deep Linking, Names and Role Provisioning, and Assignment and Grade Services independently and SHALL require the exact registered OAuth scopes for each operation.

#### Scenario: Grade write lacks scope
- **WHEN** a caller requests an AGS score write without the required scope
- **THEN** the request is denied and no grade changes

#### Scenario: Duplicate score delivery
- **WHEN** the same external score operation is retried
- **THEN** one bounded grade update is applied and the repeated outcome is auditable
