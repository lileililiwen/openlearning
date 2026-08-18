## ADDED Requirements

### Requirement: User can manage their profile

The system SHALL allow a signed-in user to edit their display name, bio, and avatar on a profile page.

#### Scenario: Edit profile
- **WHEN** a signed-in user saves changes to their display name, bio, or avatar
- **THEN** their profile is updated

#### Scenario: View own profile
- **WHEN** a user opens their profile page
- **THEN** their current profile values are shown

### Requirement: User can change and reset their password

The system SHALL allow a signed-in user to change their password (with the current password) and SHALL provide a forgot-password flow that issues a reset token.

#### Scenario: Change password
- **WHEN** a signed-in user changes their password with the correct current password
- **THEN** the password is updated and the user can sign in with the new password

#### Scenario: Reset forgotten password
- **WHEN** a user requests a password reset and follows the reset link with a valid token
- **THEN** the password is updated

### Requirement: Instructor has a public page

The system SHALL provide a public page for each Instructor showing their name, bio, avatar, and published courses.

#### Scenario: View instructor page
- **WHEN** any visitor opens an instructor's public page
- **THEN** the instructor's bio and published courses are shown
