# course-management Specification

## Purpose
TBD - created by archiving change initial-lms-mvp. Update Purpose after archive.
## Requirements
### Requirement: Instructor can create a course

The system SHALL allow an Instructor to create a course with a title, description, and category, and set it to draft state.

#### Scenario: Instructor creates a draft course
- **WHEN** an Instructor submits a new course form with a title and description
- **THEN** a course is created in Draft state owned by that Instructor
- **THEN** the Instructor is redirected to the course edit page

### Requirement: Instructor can edit own course

The system SHALL allow an Instructor to edit the title, description, and category of courses they own. Non-owners MUST NOT be able to edit.

#### Scenario: Owner edits course
- **WHEN** the owning Instructor saves changes to a course
- **THEN** the course metadata is updated

#### Scenario: Non-owner edit is denied
- **WHEN** an Instructor who does not own the course attempts to edit it
- **THEN** the system SHALL deny access

### Requirement: Course publish lifecycle

The system SHALL track course state as Draft or Published. Only Published courses are visible to Students in the catalog. Instructors can publish and unpublish their own courses.

#### Scenario: Publish course
- **WHEN** an Instructor publishes their draft course
- **THEN** the course state becomes Published
- **THEN** the course appears in the public catalog

#### Scenario: Unpublish course
- **WHEN** an Instructor unpublishes their published course
- **THEN** the course state becomes Draft
- **THEN** the course disappears from the public catalog

### Requirement: Admin can manage all courses

The system SHALL allow an Admin to view and delete any course regardless of ownership.

#### Scenario: Admin deletes any course
- **WHEN** an Admin deletes a course
- **THEN** the course and its modules, lessons, and enrollments are removed

