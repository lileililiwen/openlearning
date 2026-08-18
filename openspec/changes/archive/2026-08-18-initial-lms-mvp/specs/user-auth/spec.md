## ADDED Requirements

### Requirement: User can register

The system SHALL allow a new user to register with an email and password via ASP.NET Core Identity.

#### Scenario: Successful registration
- **WHEN** a user submits a valid email and password on the register page
- **THEN** an Identity user is created with the default Student role
- **THEN** the user is signed in and redirected to their dashboard

#### Scenario: Duplicate email registration
- **WHEN** a user registers with an email already in use
- **THEN** the system SHALL display a validation error and not create an account

### Requirement: User can sign in and sign out

The system SHALL allow registered users to sign in with email and password and to sign out.

#### Scenario: Successful sign in
- **WHEN** a registered user submits correct credentials
- **THEN** the user is signed in and redirected to their dashboard

#### Scenario: Failed sign in
- **WHEN** a user submits incorrect credentials
- **THEN** the system SHALL show an error and stay on the sign-in page

#### Scenario: Sign out
- **WHEN** an authenticated user clicks sign out
- **THEN** the session is terminated and the user is redirected to the home page

### Requirement: Role-based access control

The system SHALL enforce three roles — Student, Instructor, and Admin — and restrict access to role-specific pages.

#### Scenario: Admin page access denied for non-admin
- **WHEN** a Student or Instructor navigates to an admin-only page
- **THEN** the system SHALL deny access with a 403/redirect

#### Scenario: Instructor-only page access denied for student
- **WHEN** a Student navigates to an instructor-only page
- **THEN** the system SHALL deny access with a 403/redirect

#### Scenario: Public pages require no authentication
- **WHEN** an anonymous user browses the course catalog and home page
- **THEN** the pages render without requiring sign-in