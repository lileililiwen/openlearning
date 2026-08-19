# menu-config Specification

## Purpose
TBD - created by archiving change navigation-chrome. Update Purpose after archive.
## Requirements
### Requirement: Admin can manage menu groups and items

The system SHALL allow an Admin to create, rename, reorder, and hide menu groups and items under `Admin / System / Menu`. Changes SHALL take effect on the next page load for every user.

#### Scenario: Add menu item

- **WHEN** an Admin adds an item with label, route, icon, and allowed roles
- **THEN** the item appears in the sidebar for users with a matching role after the next page load

#### Scenario: Rename a group

- **WHEN** an Admin renames a menu group
- **THEN** the new label is shown in every user's sidebar

#### Scenario: Reorder groups

- **WHEN** an Admin changes the sort order of groups
- **THEN** the sidebar reflects the new order

#### Scenario: Hide an item

- **WHEN** an Admin marks an item as hidden
- **THEN** no user sees the item in their sidebar until it is unhidden

#### Scenario: Restrict item to a role

- **WHEN** an Admin sets an item's allowed roles to `Admin`
- **THEN** only Admin users see that item; other roles do not

### Requirement: Menu tree is stored in system-config

The system SHALL persist the menu tree under the `navigation.menu.v1` key in the existing system-config JSON store, so no new EF migration is required and existing config retention rules apply.

#### Scenario: Read menu on each request

- **WHEN** a page request is being rendered
- **THEN** the navigation module reads `navigation.menu.v1` and merges it with built-in defaults

#### Scenario: Missing key falls back to defaults

- **WHEN** `navigation.menu.v1` is unset
- **THEN** the built-in default menu is used

#### Scenario: Invalid JSON falls back to defaults

- **WHEN** `navigation.menu.v1` contains invalid JSON
- **THEN** the navigation module logs an error and uses the built-in defaults

