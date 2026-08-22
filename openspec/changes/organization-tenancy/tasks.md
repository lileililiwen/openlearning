# Organization Tenancy — Tasks

## 1. Security model

- [ ] 1.1 Add the Organizations project, organization/department/membership models, and configurations
- [ ] 1.2 Implement trusted organization context, scoped authorization handlers, and deny-by-default service helpers
- [ ] 1.3 Implement hierarchy validation, invitations, membership lifecycle, and tenant switching
- [ ] 1.4 Add database registration and an EF Core migration

## 2. Tenant workflows

- [ ] 2.1 Add Platform Admin organization provisioning, suspension, and audit pages
- [ ] 2.2 Add Organization Admin hierarchy, membership, scoped-role, and course-assignment pages
- [ ] 2.3 Add active-organization selection and visibly scoped navigation

## 3. Verification

- [ ] 3.1 Add cross-tenant read/write/ID-forgery and background-job isolation tests
- [ ] 3.2 Build cleanly and exercise every role, suspension, and isolation scenario over HTTP
