# learner-notes Specification

## Purpose
TBD - created by archiving change learner-notes. Update Purpose after archive.
## Requirements
### Requirement: Learners create contextual private notes

The system SHALL allow a Student to create a private note linked to a visible course, lesson, resource, or media timestamp, with optional tags.

#### Scenario: Note at a video timestamp
- **WHEN** an enrolled Student saves a note while viewing a lesson video
- **THEN** the note records the lesson context and current media offset

#### Scenario: Inaccessible context
- **WHEN** a Student tries to attach a note to content they cannot access
- **THEN** the system SHALL reject the request without disclosing content details

### Requirement: Notes remain owner-private and safely rendered

The system SHALL restrict every note read and mutation to its owner and SHALL sanitize supported formatting before rendering.

#### Scenario: Foreign note identifier
- **WHEN** a user requests or mutates another learner's note identifier
- **THEN** the system returns not found and does not reveal note metadata

#### Scenario: Unsafe markup
- **WHEN** a learner saves scriptable or disallowed markup
- **THEN** the rendered note contains no executable content

### Requirement: Learners organize and search notes

The system SHALL allow the owner to filter notes by context and tag and search note bodies.

#### Scenario: Search notes
- **WHEN** a learner searches for a term
- **THEN** only matching notes owned by that learner are returned

### Requirement: Learners export and delete notes

The system SHALL allow a learner to export all owned notes in a portable format and permanently delete an individual note after confirmation.

#### Scenario: Deleted context
- **WHEN** referenced learning content has been removed
- **THEN** the note remains exportable with a deleted-context marker

