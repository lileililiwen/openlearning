# lesson-preview Specification

## Purpose
TBD - created by archiving change lesson-preview. Update Purpose after archive.
## Requirements
### Requirement: Instructor can mark lessons as preview

The system SHALL allow an Instructor to mark a lesson of their published course as a preview, making it visible and accessible to non-enrolled visitors.

#### Scenario: Mark lesson as preview
- **WHEN** an Instructor saves a lesson with the preview flag
- **THEN** the lesson is accessible to non-enrolled visitors of the published course

#### Scenario: Preview badge on details
- **WHEN** a non-enrolled visitor views a course's details
- **THEN** preview lessons are shown with a badge and are linked

#### Scenario: Non-preview stays gated
- **WHEN** a non-enrolled visitor tries to open a lesson that is not a preview
- **THEN** access is denied as before

### Requirement: Preview viewing does not record progress

The system SHALL NOT record progress or last-access for a non-enrolled user viewing a preview lesson.

#### Scenario: Preview progress
- **WHEN** a non-enrolled user views a preview lesson
- **THEN** no progress, completion, or last-access is recorded for that user

