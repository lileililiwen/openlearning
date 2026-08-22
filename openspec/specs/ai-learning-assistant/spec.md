# ai-learning-assistant Specification

## Purpose
TBD - created by archiving change ai-learning-assistant. Update Purpose after archive.
## Requirements
### Requirement: AI features are explicitly governed

The system SHALL keep AI features disabled by default and SHALL let an Admin configure approved providers/models, feature scope, quotas, retention, and external-processing disclosure without exposing provider secrets.

#### Scenario: AI is disabled
- **WHEN** a user requests an AI feature that is not enabled for their scope
- **THEN** no provider call occurs and the feature reports that it is unavailable

### Requirement: Course answers are authorized and grounded

The system SHALL answer learner questions only from currently authorized, instructor-approved course sources and SHALL provide citations or state that the approved sources are insufficient.

#### Scenario: Source belongs to another course
- **WHEN** retrieval finds content outside the learner's authorized course scope
- **THEN** that content is excluded before any provider request is constructed

#### Scenario: Insufficient evidence
- **WHEN** approved sources do not support an answer
- **THEN** the assistant states the limitation and offers instructor/Q&A escalation rather than inventing an answer

### Requirement: AI grading remains advisory

The system SHALL treat rubric feedback and score suggestions as drafts and SHALL not alter an official grade until an authorized human grader confirms the final values.

#### Scenario: Provider returns a score
- **WHEN** an AI provider proposes a score for subjective work
- **THEN** the score remains unpublished and has no grade effect until human confirmation

### Requirement: AI use is transparent, safe, and auditable

The system SHALL label generated output, minimize transmitted personal data, apply safety checks, record provider/model/usage metadata, and permit users to report problematic output.

#### Scenario: Provider failure
- **WHEN** the provider times out or rejects a request
- **THEN** no partial grading action is committed and the user receives a safe retryable error
