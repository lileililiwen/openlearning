## ADDED Requirements

### Requirement: Managers create versioned cross-course learning paths

The system SHALL allow an Admin or authorized Instructor to compose a learning path from ordered stages containing required courses and elective groups, and SHALL validate that referenced courses exist and that prerequisite relationships are acyclic.

#### Scenario: Publish a valid path
- **WHEN** an authorized manager publishes a valid draft path
- **THEN** the system creates an immutable published version available to eligible learners

#### Scenario: Reject a prerequisite cycle
- **WHEN** a manager introduces a direct or indirect prerequisite cycle
- **THEN** the system SHALL reject publication and identify the cycle

### Requirement: Learners follow prerequisites without bypassing commerce

The system SHALL show a learner which path courses are available, blocked, in progress, or complete, and SHALL require normal enrollment or purchase rules before course access is granted.

#### Scenario: Prerequisite blocks a course
- **WHEN** a learner has not completed a required predecessor
- **THEN** the successor is shown as blocked and cannot be started through the path

#### Scenario: Eligible paid course
- **WHEN** a learner becomes eligible for a paid course
- **THEN** the path links to the normal purchase flow and does not grant enrollment automatically

### Requirement: Path completion honors required and elective rules

The system SHALL mark a path complete only when all required courses and each elective group's minimum selection count are complete.

#### Scenario: Elective threshold met
- **WHEN** the learner completes every required course and enough courses in every elective group
- **THEN** the assigned path version is marked complete with a completion timestamp

### Requirement: Published changes do not rewrite active assignments

The system SHALL assign new path versions only to new path enrollments unless an authorized manager explicitly migrates an existing learner.

#### Scenario: Publish a revised path
- **WHEN** a manager publishes changes while learners are active on the previous version
- **THEN** those learners retain their original requirements
