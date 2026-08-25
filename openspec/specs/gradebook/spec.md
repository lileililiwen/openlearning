# gradebook Specification

## Purpose

Per-course weighted gradebook aggregating assignment, quiz, and exam scores into one course grade — with overrides, excusals, publication gating, and student-scoped visibility — while source-of-record scores remain in their owning modules.
## Requirements
### Requirement: Instructor configures weighted gradebook items

The system SHALL allow the owning Instructor to build a course gradebook from graded assignments, quizzes, and exams with percentage weights, SHALL require the active weights to total exactly 100% before publication, and SHALL deny configuration to non-owners.

#### Scenario: Configure items
- **WHEN** the owning Instructor adds assignments, a quiz, and an exam with weights summing to 100%
- **THEN** the gradebook is valid and can compute aggregates

#### Scenario: Weights do not total 100
- **WHEN** the Instructor attempts to publish with weights totaling less or more than 100%
- **THEN** the system SHALL reject the action and explain the discrepancy

#### Scenario: Non-owner denied
- **WHEN** an Instructor who does not own the course opens or edits the gradebook
- **THEN** the system SHALL deny access

### Requirement: Aggregates are computed from graded scores only

The system SHALL compute each enrolled student's aggregate as the weight-normalized mean of item scores that have been graded, ignoring ungraded items without treating them as zero.

#### Scenario: Partial grading
- **WHEN** a student has grades in items worth 60% of the total weight
- **THEN** the aggregate is the weighted mean of those graded items alone

#### Scenario: Recompute after new grade
- **WHEN** an Instructor grades another item
- **THEN** the student's aggregate reflects the new score on next view

### Requirement: Overrides and excusals are explicit and auditable

The system SHALL allow the owning Instructor to override any student's item score and to excuse a student from an item, where excused items are excluded from both numerator and denominator, and all overrides and excusals record who made them and when.

#### Scenario: Excuse a student
- **WHEN** the Instructor excuses a student from an exam
- **THEN** the aggregate is computed over the remaining weights only

#### Scenario: Override an item score
- **WHEN** the Instructor sets an override score for an item
- **THEN** the aggregate uses the override while the original source score remains visible in its module

### Requirement: Student visibility is gated by publication

The system SHALL hide the gradebook from students until the owning Instructor publishes it, SHALL show each student only their own item scores, aggregate, and applied overrides/excusals after publication, and SHALL keep unpublished changes invisible to students.

#### Scenario: Publish then view
- **WHEN** the Instructor publishes the gradebook and a student opens their course grades
- **THEN** the student sees their item scores and current aggregate

#### Scenario: Unpublished remains hidden
- **WHEN** the gradebook is not published and a student attempts to open course grades
- **THEN** the system SHALL not disclose any gradebook data

#### Scenario: Peer data never exposed
- **WHEN** a student views the published gradebook
- **THEN** only that student's own rows are returned

