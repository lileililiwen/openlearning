# Organization Tenancy — Design

## Context

Tenant isolation is a security boundary, not a UI filter. The platform also needs to retain global B2C users and courses.

## Goals

- Model organizations and bounded department trees.
- Scope delegated roles and data access to an active organization.
- Make isolation testable and fail closed.

## Non-Goals

- One database per tenant or custom code deployments.
- Converting all existing platform data into tenant-owned data.

## Decisions

## Existing Components Reused

- Reuse ASP.NET Core authorization handlers and policies from `OpenLearning.Auth` for scoped-role checks.
- Reuse `ApplicationUser`, `Course`, the base `DbContext` module pattern, Razor Pages antiforgery, and the existing Admin ownership/role-gating conventions.
- Reuse the protected-cookie approach from `NavPreferencesService` for active-organization selection; the cookie is only a selector and every request revalidates it against active membership.
- Keep tenant audit records in the Organizations module while following the append-only operation-log pattern.

### D1: Shared database with explicit scope

Organization-owned rows carry a non-null `OrganizationId`; global rows remain null only where explicitly supported. Services require an `OrganizationContext` and never accept a client-supplied scope as authority.

### D2: Scoped memberships and roles

`OrganizationMembership` assigns OrganizationAdmin, Instructor, Manager, or Learner roles within one organization. Platform Admin remains global.

### D3: Hierarchy closure validation

Departments use parent IDs with cycle/depth validation. Moving a node is transactional.

### D4: Defense in depth

Use service predicates, authorization handlers, compound organization indexes, and cross-tenant integration tests. Background jobs iterate explicit tenant scopes.

## Risks / Trade-offs

- A missed predicate can leak data; deny-by-default APIs and isolation tests are mandatory.
- Shared-schema tenancy is operationally simpler but cannot offer physical isolation.

## Migration Plan

Add organization tables first. Existing data stays global; tenant adoption is explicit and audited.
