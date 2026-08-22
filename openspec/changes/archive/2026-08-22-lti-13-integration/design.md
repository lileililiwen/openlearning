# LTI 1.3 Integration — Design

## Context

LTI 1.3 is a security protocol as well as an interoperability contract. Launches require issuer/deployment validation, nonce/state replay defense, signature verification, and scoped OAuth tokens.

## Goals

- Support standards-compliant resource-link and deep-link launches.
- Map external contexts/users safely without silently granting global roles.
- Offer optional NRPS and AGS services with explicit scope and idempotency.

## Non-Goals

- LTI 1.1, arbitrary SSO, automatic account merging by display name, or importing whole external courses.

## Decisions

### Existing components reused

The module follows the existing base-`DbContext` service/configuration pattern,
uses `Course` for explicit context mappings, `AssignmentSubmission` for bounded
grade updates, the existing Admin authorization policy for management pages,
and the established Razor Pages composition conventions. It does not duplicate
identity, course ownership, assignment grading, or authorization infrastructure.

### D1: Registration and deployment boundary

Persist issuer/client/key endpoints plus one or more enabled deployment IDs. Map external context identifiers explicitly to OpenLearning courses.

### D2: Strict launch validation

Validate HTTPS issuer/audience/deployment/message type/version, signature/JWKS, timestamp, state, and one-time nonce. Cache keys within HTTP policy and fail closed.

### D3: Stable subject mapping

Map users by registration, deployment, and LTI subject. Email never auto-links accounts without a verified consent workflow. LTI roles map only to scoped course permissions.

### D4: Least-privilege services

NRPS/AGS calls require enabled capabilities and exact OAuth scopes. Grade writes are idempotent, bounded to mapped line items, and audited.

## Risks / Trade-offs

- Platform interoperability varies; conformance fixtures and provider-specific diagnostics are required without weakening validation.
- External identifiers and grades are personal data; retention and disconnect handling must be configurable.

## Migration Plan

Add registration, deployment, context, subject, resource-link, line-item, nonce/state, key, and audit tables. No registrations exist by default.
