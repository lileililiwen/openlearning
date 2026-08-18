## MODIFIED Requirements

### Requirement: Instructor can create a course

The system SHALL allow an Instructor to create a course with a title, description, category, an optional price, and optional metadata (level, duration, language, prerequisites, outcomes), and set it to draft state.

#### Scenario: Instructor creates a draft course
- **WHEN** an Instructor submits a new course form with a title and description
- **THEN** a course is created in Draft state owned by that Instructor
- **THEN** the Instructor is redirected to the course edit page

#### Scenario: Instructor sets a price
- **WHEN** an Instructor includes a price greater than zero on a new course
- **THEN** the course is stored with that price

#### Scenario: Instructor sets course metadata
- **WHEN** an Instructor includes level, duration, language, prerequisites, or outcomes
- **THEN** the course is stored with those metadata values

### Requirement: Instructor can edit own course

The system SHALL allow an Instructor to edit the title, description, category, price, and metadata of courses they own. Non-owners MUST NOT be able to edit.

#### Scenario: Owner edits course
- **WHEN** the owning Instructor saves changes to a course
- **THEN** the course metadata and price are updated

#### Scenario: Non-owner edit is denied
- **WHEN** an Instructor who does not own the course attempts to edit it
- **THEN** the system SHALL deny access
