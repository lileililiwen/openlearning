# instructor-onboarding Specification

## Purpose
TBD - created by archiving change user-management. Update Purpose after archive.
## Requirements
### Requirement: User can apply to become an instructor

The system SHALL allow any registered user to submit an instructor application with a motivation statement, and SHALL keep one application per user.

#### Scenario: Submit application
- **WHEN** a signed-in user submits an instructor application
- **THEN** a pending application is stored for that user and shown as pending on their apply page

#### Scenario: Duplicate application is replaced
- **WHEN** a user with an existing application submits again
- **THEN** the previous application is replaced by the new one

### Requirement: Admin can approve or reject applications

The system SHALL allow an Admin to review pending instructor applications, approve them (granting the `Instructor` role) or reject them with an optional reason.

#### Scenario: Approve application
- **WHEN** an Admin approves a pending application
- **THEN** the applicant gains the `Instructor` role and the application is marked approved

#### Scenario: Reject application
- **WHEN** an Admin rejects a pending application
- **THEN** the application is marked rejected and the applicant does not gain the role

