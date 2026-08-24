## ADDED Requirements

### Requirement: Mobile clients use a versioned authorized API

The system SHALL expose versioned mobile DTO endpoints backed by existing domain services, with consistent errors, cursor pagination, rate limits, and documented compatibility.

#### Scenario: Domain authorization applies
- **WHEN** a mobile client requests a course resource
- **THEN** the same enrollment, purchase, ownership, and access-period rules as the web application are enforced

### Requirement: Device sessions are revocable

The system SHALL issue short-lived access tokens and rotating refresh tokens per device, store refresh secrets only as hashes, and revoke a token family on detected reuse.

#### Scenario: Reused refresh token
- **WHEN** a rotated refresh token is presented again
- **THEN** the device token family is revoked and the security event is audited

### Requirement: Authorized content can be prepared for offline use

The system SHALL create an expiring offline manifest only for downloadable content currently accessible to the learner and SHALL support checksum-verified resumable downloads.

#### Scenario: Access expires before download
- **WHEN** course access expires before an asset request completes
- **THEN** further asset authorization is denied even if the manifest has not expired

### Requirement: Offline mutations synchronize idempotently

The system SHALL accept client operation identifiers, return the prior outcome for retries, preserve monotonic progress, and report editable-record conflicts with canonical state.

#### Scenario: Progress retry
- **WHEN** a device retries the same lesson-completion operation
- **THEN** one completion is recorded and the same canonical outcome is returned

### Requirement: Native push endpoints follow device lifecycle

The system SHALL allow an authenticated device to register, replace, and remove its own push endpoint and SHALL disable endpoints rejected permanently by a provider.

#### Scenario: Device logout
- **WHEN** a learner logs out a device
- **THEN** that device session and push endpoint are revoked without affecting other devices
