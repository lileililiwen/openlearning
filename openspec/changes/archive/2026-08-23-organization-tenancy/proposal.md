## Why

Class groups organize learners within courses but do not model customer organizations, departmental hierarchy, delegated administration, or tenant data boundaries.

## What Changes

- Add organizations, hierarchy, memberships, scoped roles, and tenant course assignments.
- Require tenant scoping for organization-owned data and delegated administration.
- Add tenant switching, branding/configuration hooks, audit records, and lifecycle controls.

## Capabilities

### New Capabilities
- `organization-tenancy`: B2B organizations, hierarchy, scoped authorization, and isolation.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Organizations` domain.
- Later implementation must add explicit organization scope to opted-in modules; global B2C data remains unscoped.
