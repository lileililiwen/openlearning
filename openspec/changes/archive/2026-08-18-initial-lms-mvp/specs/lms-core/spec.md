## ADDED Requirements

### Requirement: Application provides shared navigation

The system SHALL provide a shared layout with navigation that adapts to the authenticated user's roles.

#### Scenario: Instructor sees instructor links
- **WHEN** an Instructor is signed in
- **THEN** the navigation shows links to manage their courses

#### Scenario: Admin sees admin links
- **WHEN** an Admin is signed in
- **THEN** the navigation shows links to the admin console

### Requirement: Seed data on first run

The system SHALL seed an admin account and a sample published course with modules and lessons on first startup so the MVP is immediately demonstrable.

#### Scenario: First-run seeding
- **WHEN** the application starts with an empty database
- **THEN** an Admin user and one sample published course with at least one module and lesson are created
- **THEN** an Instructor user exists to manage the sample course

### Requirement: MIT license and attribution

The system SHALL be released under the MIT license and SHALL credit MIT-licensed open-source reference projects in the README in accordance with open-source spirit.

#### Scenario: README includes credits
- **WHEN** a developer reads the README
- **THEN** it includes an Acknowledgments section listing CoreLMS, SmartLearning, and LearnNest with their sources