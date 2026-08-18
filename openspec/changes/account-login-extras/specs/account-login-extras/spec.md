## ADDED Requirements

### Requirement: User can sign in with phone and verification code

The system SHALL allow a user to register and sign in using a phone number plus a one-time verification code.

#### Scenario: Phone sign-in with new number
- **WHEN** a user enters a phone number and verifies a valid one-time code
- **THEN** a Student account is created if none exists and the user is signed in

#### Scenario: Phone sign-in with existing number
- **WHEN** a user with an existing account verifies a valid code for their phone
- **THEN** the user is signed in to their existing account

#### Scenario: Invalid or expired code
- **WHEN** a user submits an incorrect, expired, or already-used code
- **THEN** the system SHALL reject the sign-in and the code is not reusable

### Requirement: User can sign in with a third-party provider

The system SHALL allow a user to sign in through a configured OAuth provider (Google/GitHub).

#### Scenario: OAuth sign-in
- **WHEN** a user completes the provider's OAuth flow
- **THEN** the user is signed in, linked to an existing account by email, or a new Student account is created

#### Scenario: OAuth not configured
- **WHEN** no provider is configured in appsettings
- **THEN** the third-party sign-in option is hidden and email/password sign-in still works
