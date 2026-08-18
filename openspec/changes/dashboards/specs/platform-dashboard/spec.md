## ADDED Requirements

### Requirement: Admin has a platform dashboard

The system SHALL provide a dashboard for Admins showing platform-wide metrics and links into operational surfaces.

#### Scenario: Admin views platform dashboard
- **WHEN** an Admin opens the platform dashboard
- **THEN** it shows counts of students, instructors, courses (draft/published), enrollments, paid revenue, and completion rate

#### Scenario: Recent activity
- **WHEN** an Admin views the dashboard
- **THEN** it lists recent signups, recent courses, and recent orders, each linking to the relevant management surface

### Requirement: Signed-in users land on their dashboard

The system SHALL redirect authenticated users from the home page to their role's dashboard.

#### Scenario: Student lands on dashboard
- **WHEN** a signed-in Student navigates to the home page
- **THEN** they are redirected to the student dashboard

#### Scenario: Anonymous visitor still sees the catalog
- **WHEN** an anonymous user visits the home page
- **THEN** the public catalog is shown with a sign-in CTA
