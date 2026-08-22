## ADDED Requirements

### Requirement: Credits are awarded through an auditable ledger

The system SHALL record credit awards with learner, amount, category, source, rule version, timestamp, and actor, and SHALL make source processing idempotent.

#### Scenario: Course completion awards credit once
- **WHEN** the same qualifying course-completion event is processed more than once
- **THEN** exactly one credit award exists for that source and learner

#### Scenario: Correct an award
- **WHEN** an authorized Admin revokes an incorrect award with a reason
- **THEN** a compensating ledger entry is recorded and the original entry remains auditable

### Requirement: Graduation rules are versioned

The system SHALL allow an Admin to publish a program version containing required courses, minimum total credits, category minimums, and optional credit-expiry rules.

#### Scenario: Program rules change
- **WHEN** a new program version is published
- **THEN** existing learners retain their assigned version unless explicitly migrated

### Requirement: Learners can inspect graduation eligibility

The system SHALL show each learner earned credits, applicable program rules, satisfied requirements, and every unmet requirement.

#### Scenario: Learner is not eligible
- **WHEN** a learner is below a category minimum
- **THEN** the degree audit identifies that category and the remaining amount

### Requirement: Graduation is an explicit authorized decision

The system SHALL permit an Admin to mark a learner graduated only after a current evaluation reports all requirements satisfied.

#### Scenario: Stale eligibility is rejected
- **WHEN** a previously eligible learner no longer satisfies the current assigned rules at decision time
- **THEN** graduation is rejected with the unmet requirements
