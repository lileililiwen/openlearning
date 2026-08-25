## ADDED Requirements

### Requirement: Authors create surveys with structured questions

The system SHALL allow an owning Instructor (course scope) or an Admin (platform scope) to create surveys with single-choice, multiple-choice, rating-scale, and open-text questions, optional open and close times, and an anonymity mode.

#### Scenario: Create course survey
- **WHEN** the owning Instructor publishes a survey with mixed question types and a close time
- **THEN** enrolled students can respond until it closes

#### Scenario: Non-owner cannot manage
- **WHEN** an Instructor who does not own the course attempts to edit or delete its survey
- **THEN** the system SHALL deny access

### Requirement: Eligible users respond once within the window

The system SHALL accept at most one response per user per survey, only from enrolled students in course scope or any authenticated user in platform scope, and only while the window is open.

#### Scenario: Submit response
- **WHEN** an eligible user submits completed answers during the window
- **THEN** the response is stored for that survey

#### Scenario: Second response rejected
- **WHEN** the same user submits again
- **THEN** the system SHALL reject the duplicate

#### Scenario: Window closed
- **WHEN** a user attempts to respond after close
- **THEN** the submission is rejected

#### Scenario: Not enrolled
- **WHEN** a non-enrolled user attempts to respond to a course survey
- **THEN** the system SHALL deny access

### Requirement: Anonymity is enforced end to end

The system SHALL store anonymous responses without respondent identity linkage, SHALL prevent authors from viewing individual anonymous responses, and SHALL show attributed responses individually only when anonymity is off.

#### Scenario: Anonymous results
- **WHEN** an author views results of an anonymous survey
- **THEN** only aggregate statistics are shown with no respondent identities

### Requirement: Results are aggregated and policy-gated

The system SHALL present per-question aggregates — counts and percentages for choices and ratings, listed text for open questions — and SHALL reveal results to the author only after the survey closes unless the author explicitly enables live results.

#### Scenario: Results after close
- **WHEN** the survey closes and the author opens results
- **THEN** aggregates for every question are shown

#### Scenario: Live results disabled by default
- **WHEN** live results were not enabled and the survey is still open
- **THEN** the author sees response count only, not answer content

### Requirement: Surveys never affect academic records

The system SHALL keep survey participation and results separate from grades, progress, credits, certificates, and gamification scoring.

#### Scenario: Response recorded
- **WHEN** a student responds to a survey
- **THEN** no grade, progress, credit, certificate, or points record changes
