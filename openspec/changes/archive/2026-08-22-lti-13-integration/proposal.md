## Why

SCORM imports courseware but does not let external learning platforms securely launch OpenLearning tools, provision users/roles, deep-link content, or exchange grades using LTI 1.3.

## What Changes

- Add LTI 1.3 platform registration, OIDC login initiation, signed launch validation, and key rotation.
- Add Names and Role Provisioning, Deep Linking, and Assignment and Grade Services with least privilege.
- Add deployment/course-context mapping, audit, revocation, and replay protection.

## Capabilities

### New Capabilities
- `lti-13-integration`: standards-based external LMS launches, deep links, roster access, and grade return.

### Modified Capabilities
- None.

## Impact

- New `OpenLearning.Lti` boundary integrating with Auth, courses, enrollment, assignments/assessments, and grade services.
