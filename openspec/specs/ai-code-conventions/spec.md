# ai-code-conventions Specification

## Purpose
TBD - created by archiving change ai-code-conventions. Update Purpose after archive.
## Requirements
### Requirement: AI involvement is recorded

The system SHALL record whether a change is AI-generated, AI-assisted, or human-authored in the pull request.

#### Scenario: Marker present
- **WHEN** a contributor opens a pull request
- **THEN** the PR records AI involvement (generated, assisted, or none)

### Requirement: AI-marked code is reviewed with extra care

The system SHALL require an AI-specific review checklist for generated or assisted code.

#### Scenario: AI review checklist
- **WHEN** a PR is marked generated or assisted
- **THEN** the review confirms spec compliance, authorization/ownership checks, injection safety, test coverage or a stated reason, and no dead code

#### Scenario: Large unmarked diff
- **WHEN** a PR adds a large amount of code without any AI marker
- **THEN** a soft warning comment is posted (non-blocking)

