## ADDED Requirements

### Requirement: Admin manages course categories

The system SHALL allow an Admin to create, rename, deactivate, and order categories, and SHALL make courses select from the managed list.

#### Scenario: Create category
- **WHEN** an Admin creates a category
- **THEN** the category is available in course forms and catalog filters

#### Scenario: Rename cascades
- **WHEN** an Admin renames a category
- **THEN** courses assigned to it reflect the new name

#### Scenario: Deactivate category
- **WHEN** an Admin deactivates a category
- **THEN** it no longer appears in forms or filters but existing courses keep their value

### Requirement: Admin manages tags

The system SHALL allow an Admin to rename, merge, and retire tags.

#### Scenario: Rename tag
- **WHEN** an Admin renames a tag
- **THEN** all courses tagged with it use the new name

#### Scenario: Merge tags
- **WHEN** an Admin merges one tag into another
- **THEN** courses carrying the source tag now carry the target tag and the source is removed

#### Scenario: Retire tag
- **WHEN** an Admin retires a tag
- **THEN** the tag is hidden from filters and forms but remains on existing courses
