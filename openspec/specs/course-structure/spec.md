# course-structure Specification

## Purpose
TBD - created by archiving change initial-lms-mvp. Update Purpose after archive.
## Requirements
### Requirement: Course contains ordered modules

The system SHALL allow an Instructor to organize a course's content into Modules, each with a title and a display order within the course.

#### Scenario: Add module
- **WHEN** an Instructor adds a module to their course
- **THEN** a module is created with the next highest order position
- **THEN** the module appears in the course structure

### Requirement: Module contains ordered lessons

The system SHALL allow an Instructor to add Lessons to a Module. Each lesson has a title and an order within its module.

#### Scenario: Add lesson
- **WHEN** an Instructor adds a lesson with a title to a module
- **THEN** a lesson is created with the next highest order position in that module

### Requirement: Content is gated by course ownership

The system SHALL restrict module/lesson creation and editing to the Instructor who owns the course. Students can only view structure of courses they are enrolled in.

#### Scenario: Non-owner cannot add content
- **WHEN** an Instructor who does not own the course attempts to add a module or lesson
- **THEN** the system SHALL deny access

#### Scenario: Student views enrolled course structure
- **WHEN** an enrolled Student opens a course
- **THEN** the course is shown with its modules and lessons in order

