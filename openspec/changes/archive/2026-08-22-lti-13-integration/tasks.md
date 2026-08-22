# LTI 1.3 Integration — Tasks

## 1. Protocol foundation

- [x] 1.1 Add the Lti project, registration/deployment/mapping/key/nonce/audit models, configurations, and DI registration
- [x] 1.2 Implement OIDC login initiation and resource-link launch validation with replay defense and safe JWKS caching
- [x] 1.3 Implement scoped subject/role/context mapping and registration revocation
- [x] 1.4 Add database registration and an EF Core migration

## 2. LTI Advantage services

- [x] 2.1 Implement Deep Linking response generation and resource-link management
- [x] 2.2 Implement optional NRPS roster synchronization with tenant/course scope
- [x] 2.3 Implement optional AGS line items and idempotent score return with exact OAuth scopes
- [x] 2.4 Add Admin registration, mapping, capability, key rotation, diagnostics, and audit pages

## 3. Verification

- [x] 3.1 Test invalid signatures/audience/deployment, replay, stale keys, role escalation, scope denial, and duplicate grades
- [x] 3.2 Build cleanly and pass LTI protocol fixtures plus end-to-end sandbox launches
