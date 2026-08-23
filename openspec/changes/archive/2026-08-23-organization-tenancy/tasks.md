# Organization Tenancy — Tasks

## 1. Security model

- [x] 1.1 Add the Organizations project, organization/department/membership models, and configurations
- [x] 1.2 Implement trusted organization context, scoped authorization handlers, and deny-by-default service helpers
- [x] 1.3 Implement hierarchy validation, invitations, membership lifecycle, and tenant switching
- [x] 1.4 Add database registration and an EF Core migration

## 2. Tenant workflows

- [x] 2.1 Add Platform Admin organization provisioning, suspension, and audit pages
- [x] 2.2 Add Organization Admin hierarchy, membership, scoped-role, and course-assignment pages
- [x] 2.3 Add active-organization selection and visibly scoped navigation

## 3. Verification

- [x] 3.1 Add cross-tenant read/write/ID-forgery and background-job isolation tests
- [x] 3.2 Build cleanly and exercise every role, suspension, and isolation scenario over HTTP
