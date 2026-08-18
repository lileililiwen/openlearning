# system-config Specification

## Purpose
TBD - created by archiving change system-config. Update Purpose after archive.
## Requirements
### Requirement: Admin edits system parameters

The system SHALL allow an Admin to view and edit a whitelist of system parameters that affect application behavior, with code defaults when unset.

#### Scenario: Edit parameter
- **WHEN** an Admin changes a system parameter (e.g. catalog page size)
- **THEN** the new value is used by the application

#### Scenario: Invalid value
- **WHEN** an Admin enters an invalid value for a parameter
- **THEN** the value is rejected or the default is used

### Requirement: Admin edits notification templates

The system SHALL allow an Admin to edit title and body templates per notification type and SHALL render new notifications from the template.

#### Scenario: Edit template
- **WHEN** an Admin edits a notification template
- **THEN** subsequent notifications of that type use the new title and body

#### Scenario: Placeholders
- **WHEN** a template contains placeholders such as course title or score
- **THEN** the placeholders are replaced with the event's values at creation

#### Scenario: No template
- **WHEN** no active template exists for a notification type
- **THEN** the caller-provided text is used

