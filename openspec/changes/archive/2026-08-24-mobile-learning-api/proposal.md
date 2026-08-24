## Why

Responsive Razor Pages and browser push do not provide a stable mobile contract, token lifecycle, offline learning, or conflict-aware synchronization.

## What Changes

- Add a versioned mobile API with device sessions and scoped access tokens.
- Add downloadable offline manifests and resumable protected content downloads.
- Add idempotent progress/note synchronization and native push-device registration.

## Capabilities

### New Capabilities
- `mobile-learning-api`: secure mobile API, offline packages, synchronization, and device push.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Mobile` application boundary reusing domain services; it SHALL NOT duplicate business rules.
- Storage and notifications are consumed through their existing services.
