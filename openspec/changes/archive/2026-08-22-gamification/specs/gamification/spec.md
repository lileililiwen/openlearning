## ADDED Requirements

### Requirement: Points come only from trusted idempotent rules

The system SHALL award points from versioned server-side rules using unique source keys, enforce configured caps, and record corrections as compensating ledger entries.

#### Scenario: Completion event is retried
- **WHEN** the same qualifying event is processed again
- **THEN** no duplicate points are awarded

#### Scenario: Daily cap reached
- **WHEN** another event would exceed a rule's daily cap
- **THEN** only the permitted amount is awarded and the capped result is auditable

### Requirement: Badge awards preserve their evidence

The system SHALL issue a badge only when its published criteria version is satisfied and SHALL retain the rule version and supporting evidence.

#### Scenario: Badge rule changes later
- **WHEN** an Admin publishes revised criteria
- **THEN** an existing award retains its original evidence and version

### Requirement: Leaderboards are optional and scoped

The system SHALL exclude learners by default, use learner-selected display aliases, and restrict leaderboards to authorized platform, organization, course, or challenge scopes.

#### Scenario: Learner opts out
- **WHEN** a participating learner disables leaderboard visibility
- **THEN** the learner is removed from future projections without losing earned points

### Requirement: Gamification does not alter academic or monetary records

The system SHALL keep points, badges, and ranks separate from grades, credits, graduation eligibility, settlement, and cash value.

#### Scenario: Points are corrected
- **WHEN** an Admin corrects a points award
- **THEN** no grade, credit, payment, or instructor revenue record changes
