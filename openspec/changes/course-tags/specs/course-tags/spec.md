## ADDED Requirements

### Requirement: Courses can be tagged

The system SHALL allow an Instructor to assign zero or more tags to their course and SHALL allow the catalog to be filtered and displayed by tag.

#### Scenario: Tag a course
- **WHEN** an Instructor saves a course with tag names
- **THEN** the tags are associated with the course and shown on its card and details

#### Scenario: Filter by tag
- **WHEN** a visitor filters the catalog by a tag
- **THEN** only courses carrying that tag are shown

#### Scenario: Multiple tags
- **WHEN** a course has more than one tag
- **THEN** all tags are shown and the course matches a filter for any of them

### Requirement: Tag vocabulary is managed

The system SHALL keep a de-duplicated tag vocabulary so the same tag name refers to the same tag.

#### Scenario: Reuse tag
- **WHEN** two courses use the same tag name
- **THEN** they reference the same tag and share its slug
