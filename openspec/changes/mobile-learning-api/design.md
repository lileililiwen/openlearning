# Mobile Learning API — Design

## Context

Mobile clients need a durable contract and intermittent-connectivity semantics. Direct exposure of EF entities would couple clients to persistence.

## Goals

- Provide a versioned, least-privilege API over existing services.
- Support authorized offline content and idempotent synchronization.
- Register and revoke device-specific push endpoints.

## Non-Goals

- Building iOS, Android, or mini-program clients.
- Offline exams, purchases, or DRM guarantees.

## Decisions

### D1: Versioned DTO API

Expose `/api/mobile/v1` DTOs with cursor pagination, consistent problem details, rate limits, and OpenAPI documentation. Domain services remain authoritative.

### D2: Device-bound sessions

Use short-lived access tokens and rotating refresh tokens stored as hashes per device. Reuse disables the token family.

### D3: Offline manifests

Create expiring manifests containing authorized downloadable assets, checksums, sizes, and access expiry. Downloads are resumable and re-authorized.

### D4: Idempotent sync

Client mutations carry operation IDs. The server records outcomes and returns canonical state; progress completion is monotonic while editable notes use server versions and explicit conflicts.

## Risks / Trade-offs

- Downloaded media cannot be recalled; manifests minimize exposure and respect course-access expiry.
- API evolution requires compatibility tests and documented deprecation windows.

## Migration Plan

Add device-session, sync-operation, offline-manifest, and mobile-push tables. Existing web sessions are unchanged.
