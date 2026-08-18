## ADDED Requirements

### Requirement: Admin can search and view users

The system SHALL allow an Admin to list and search users by name or email and view a user's roles, enrollments, and owned courses.

#### Scenario: Admin searches users
- **WHEN** an Admin enters a search term on the users page
- **THEN** matching users are shown with their role(s) and status

#### Scenario: Admin views user detail
- **WHEN** an Admin opens a user's detail page
- **THEN** the user's roles, enrollments, and owned courses are shown

### Requirement: Admin can assign and revoke roles

The system SHALL allow an Admin to add or remove the `Instructor` role (and manage role membership) for any user, taking effect immediately.

#### Scenario: Promote to instructor
- **WHEN** an Admin assigns the `Instructor` role to a Student
- **THEN** the user gains instructor capabilities immediately

#### Scenario: Revoke instructor role
- **WHEN** an Admin removes the `Instructor` role from a user
- **THEN** the user loses instructor capabilities immediately

### Requirement: Admin can suspend and reactivate accounts

The system SHALL allow an Admin to suspend an account so the user cannot learn, teach, or chat, and to reactivate it.

#### Scenario: Suspend user
- **WHEN** an Admin suspends a user
- **THEN** the user is blocked from learning, teaching, and chat actions

#### Scenario: Reactivate user
- **WHEN** an Admin reactivates a suspended user
- **THEN** the user's access is restored
