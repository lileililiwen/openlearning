# organization-tenancy Specification

## Purpose
TBD - created by archiving change organization-tenancy. Update Purpose after archive.
## Requirements
### Requirement: Platform administrators manage organization lifecycles

The system SHALL allow a Platform Admin to create, configure, suspend, and reactivate an organization, and SHALL audit each lifecycle action.

#### Scenario: Suspend an organization
- **WHEN** a Platform Admin suspends an organization
- **THEN** its scoped users cannot access tenant data while global account access remains governed by global policy

### Requirement: Organizations contain validated department hierarchies

The system SHALL allow an Organization Admin to manage departments while preventing cycles and enforcing a configured maximum depth.

#### Scenario: Reject cyclic move
- **WHEN** an Organization Admin moves a department beneath its descendant
- **THEN** the system SHALL reject the move without changing the hierarchy

### Requirement: Delegated roles are tenant-scoped

The system SHALL authorize organization operations from the user's active membership and scoped role, not from a client-provided organization identifier.

#### Scenario: Forged tenant identifier
- **WHEN** a member submits an identifier belonging to another organization
- **THEN** the system SHALL deny the operation without revealing the target data

#### Scenario: Multi-organization user switches context
- **WHEN** a user selects another organization in which they have an active membership
- **THEN** subsequent scoped navigation and queries use that organization

### Requirement: Organization-owned data is isolated

The system SHALL apply organization scope to every opted-in web request, service query, export, notification, and background job.

#### Scenario: Cross-tenant export attempt
- **WHEN** an Organization Admin requests an export containing another tenant's identifiers
- **THEN** no other tenant's rows are returned or disclosed in errors

