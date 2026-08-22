## ADDED Requirements

### Requirement: Coordinators manage auditable placement lifecycles

The system SHALL allow an authorized coordinator to create a placement with learner, versioned program, host, dates, supervisor, competency plan, and explicit lifecycle status.

#### Scenario: Activate incomplete placement
- **WHEN** a coordinator tries to activate a placement without required parties or dates
- **THEN** the transition is rejected with missing requirements

### Requirement: External supervisors have placement-scoped access

The system SHALL issue expiring, revocable invitations and restrict an external supervisor to the minimum data and actions for assigned placements.

#### Scenario: Supervisor requests another placement
- **WHEN** a supervisor submits an identifier outside their assignment
- **THEN** access is denied without disclosing learner or host data

### Requirement: Hours and evidence use approval and amendment records

The system SHALL let learners submit dated hours and permitted evidence, require supervisor approval, prevent overlapping approved time, and preserve approved history through amendments.

#### Scenario: Edit approved hours
- **WHEN** a learner corrects an approved log
- **THEN** an amendment requires renewed approval and the prior value remains auditable

### Requirement: Completion requires all practical requirements

The system SHALL confirm completion only when minimum approved hours, required competencies, evaluations, and blocking-incident rules are satisfied.

#### Scenario: Unresolved safety incident
- **WHEN** a blocking incident remains unresolved
- **THEN** placement completion is denied with an authorized explanation
