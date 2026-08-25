# peer-assessment Specification

## Purpose

Structured peer review on assignments: phase-gated reviewer allocation, rubric-based peer assessments, policy-driven score combination with instructor override, and anonymity/release controls — scaling feedback beyond instructor-only grading.
## Requirements
### Requirement: Instructor configures peer review on an assignment

The system SHALL allow the owning Instructor to enable peer review on an assignment with a required number of reviews per student, an anonymity mode (anonymous or attributed), rubric questions with point scales, and phase dates for submission, review, and close.

#### Scenario: Enable peer review
- **WHEN** the owning Instructor enables peer review with 3 reviews and a review window
- **THEN** enrolled students see the peer review requirement and deadline

#### Scenario: Non-owner cannot configure
- **WHEN** an Instructor who does not own the course attempts to configure or edit peer review settings
- **THEN** the system SHALL deny access

### Requirement: Reviewer allocation is complete and self-free

The system SHALL allocate each eligible submission to distinct enrolled reviewers so that every submission receives the configured number of assessments where enrollment count permits, no student is allocated their own submission, and each allocation run is recorded and reproducible.

#### Scenario: Allocation runs at review start
- **WHEN** the review phase opens
- **THEN** each submitted student receives the configured number of peers' submissions to assess and none of them is their own

#### Scenario: Cohort too small
- **WHEN** fewer students are eligible than the configured review count requires
- **THEN** the maximum feasible number of distinct reviews is allocated and the shortfall is visible to the Instructor

### Requirement: Peer assessments follow the rubric within the phase

The system SHALL allow an allocated reviewer to submit scores and comments for every rubric question exactly once per allocation, only during the review phase, and only while enrolled in the course.

#### Scenario: Submit assessment
- **WHEN** an allocated reviewer submits all rubric scores during the review phase
- **THEN** the assessment is stored against that allocation

#### Scenario: Phase closed
- **WHEN** a reviewer attempts to submit after the review phase closes
- **THEN** the submission is rejected

#### Scenario: Unallocated student cannot review
- **WHEN** a Student who was not allocated a given submission attempts to assess it
- **THEN** the system SHALL deny access

### Requirement: Final scores combine instructor and peer input by policy

The system SHALL compute each participant's final score using the Instructor-selected strategy — instructor grade only, mean of received peer scores, or a weighted mix — apply the Instructor's manual override when present, and never let peer input alter another module's records without an explicit release.

#### Scenario: Weighted mix
- **WHEN** the strategy weights instructor grade 60% and peer average 40%
- **THEN** the final score is computed from both inputs according to the published weights

#### Scenario: Instructor overrides
- **WHEN** the Instructor sets a manual final score for a student
- **THEN** the override replaces the computed result and the original computation remains auditable

#### Scenario: Peer scores do not leak into grades early
- **WHEN** results have not been released
- **THEN** students cannot see received peer scores or final results

### Requirement: Anonymity and release controls are enforced

The system SHALL conceal reviewer identity from reviewees and reviewee identity from reviewers while anonymity mode is active, keep peer assessments hidden from students until the Instructor releases them, and record release as an auditable action.

#### Scenario: Anonymous review
- **WHEN** anonymity mode is enabled and a student views a received assessment
- **THEN** the reviewer's identity is not shown

#### Scenario: Release publishes results
- **WHEN** the Instructor releases results after the review phase closes
- **THEN** each student sees their received peer assessments and final score

#### Scenario: Release before close denied
- **WHEN** the Instructor attempts to release results before the review phase closes
- **THEN** the system SHALL deny the action

