# Authoring Versioning and Preview Specification

## ADDED Requirements

### Requirement: Draft and published revisions are isolated

The system SHALL maintain a mutable draft revision separately from the active published revision, and learner catalog and delivery queries MUST read only the active published revision.

#### Scenario: Instructor edits published course

- **WHEN** the owner edits content after publication
- **THEN** the draft changes without changing the learner-visible revision

#### Scenario: Learner views course during editing

- **WHEN** a learner opens a course while an owner has unpublished draft edits
- **THEN** the learner sees the active published revision only

### Requirement: Publishing requires validation

The system SHALL validate required metadata, ordering, and deliverable content before promoting a draft to published state.

#### Scenario: Invalid draft publish

- **WHEN** an instructor publishes a draft with validation errors
- **THEN** publication is rejected and each error is shown to the instructor

#### Scenario: Valid draft publish

- **WHEN** an instructor publishes a valid draft
- **THEN** the system atomically promotes it and records the actor and revision

### Requirement: Preview and rollback are authorized

The system SHALL allow an owning instructor to preview the draft and create a new draft from a prior published revision, while denying both operations to non-owners and learners.

#### Scenario: Owner previews draft

- **WHEN** the owner opens preview
- **THEN** the draft is rendered with an explicit preview indicator and is not added to the public catalog

#### Scenario: Owner rolls back

- **WHEN** the owner selects a prior published revision for rollback
- **THEN** a new draft is created from that revision and existing history remains unchanged

#### Scenario: Unauthorized revision operation

- **WHEN** a non-owner or learner requests preview or rollback
- **THEN** access is denied and no revision state changes
