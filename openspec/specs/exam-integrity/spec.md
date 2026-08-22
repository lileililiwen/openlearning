# exam-integrity Specification

## Purpose
TBD - created by archiving change exam-integrity. Update Purpose after archive.
## Requirements
### Requirement: Exam integrity sessions are server-authoritative

The system SHALL bind each attempt to a signed session, enforce availability and duration using server time, and accept only deduplicated monotonically sequenced evidence for that attempt.

#### Scenario: Replayed evidence batch
- **WHEN** a client resends an already accepted evidence batch
- **THEN** it is not counted twice and the prior acknowledgement is returned

#### Scenario: Client clock changes
- **WHEN** the device clock changes during an attempt
- **THEN** the server-controlled deadline remains unchanged

### Requirement: Integrity evidence is minimized and explainable

The system SHALL record only allowlisted signals such as visibility, copy/paste, heartbeat, and connectivity events, SHALL collect no audio/video/biometrics, and SHALL explain which rules contributed to risk.

#### Scenario: Risk threshold reached
- **WHEN** recorded evidence crosses a configured threshold
- **THEN** an incident is queued with contributing events and no automatic misconduct verdict or grade change

### Requirement: Accommodations are applied without disclosing diagnoses

The system SHALL snapshot authorized extra time, breaks, and event-threshold adjustments onto the attempt and SHALL expose only the operational adjustment to exam staff.

#### Scenario: Extra-time accommodation
- **WHEN** an eligible learner starts an exam
- **THEN** the server deadline includes the approved extra time

### Requirement: Integrity incidents receive human review and appeal

The system SHALL restrict evidence to authorized reviewers, audit access and decisions, notify the learner of an adverse disposition, and permit an appeal within policy.

#### Scenario: Instructor reviews another course
- **WHEN** an Instructor requests integrity evidence for a course outside their scope
- **THEN** access is denied without disclosing incident details

