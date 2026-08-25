## ADDED Requirements

### Requirement: Admins manage versioned competency frameworks

The system SHALL allow an Admin to create, edit, and archive competency frameworks containing hierarchical competencies with descriptions and an achievement scale, and SHALL version frameworks so published changes do not alter previously earned records.

#### Scenario: Create framework
- **WHEN** an Admin creates a framework with nested competencies and a scale
- **THEN** courses can be mapped against it

#### Scenario: Framework edited after use
- **WHEN** a competency is renamed or restructured after learners earned it
- **THEN** existing earned records retain their original competency version

### Requirement: Mapped activities produce evidence automatically

The system SHALL allow the owning Instructor to map courses and assignments to competencies, and SHALL record achievement evidence when a learner completes the mapped activity, using only trusted server-side completion data.

#### Scenario: Completion generates evidence
- **WHEN** a learner completes an activity mapped to a competency
- **THEN** an evidence record referencing that completion is created for the learner

#### Scenario: Unmapped completion produces nothing
- **WHEN** a learner completes an activity with no competency mapping
- **THEN** no evidence is recorded

### Requirement: Manual evidence requires approval

The system SHALL allow a learner to submit manual evidence (description and attachment) toward a competency and SHALL require Instructor or Admin review to approve or reject it before it affects the competency profile.

#### Scenario: Evidence approved
- **WHEN** a reviewer approves a manual evidence submission
- **THEN** the competency profile reflects the achieved level with the evidence attached

#### Scenario: Evidence rejected
- **WHEN** a reviewer rejects a submission with a reason
- **THEN** the learner sees the rejection reason and the profile is unchanged

### Requirement: Profiles show attainment and gaps against a target

The system SHALL present each learner's competency profile with achieved levels and evidence, and SHALL let authorized Instructors, managers, and Admins compare a learner or cohort against a target framework to list covered, partially covered, and missing competencies.

#### Scenario: Individual gap report
- **WHEN** an authorized viewer opens a learner's gap analysis against a target framework
- **THEN** every framework competency is listed as achieved, partial, or missing based on evidence

#### Scenario: Unauthorized viewer denied
- **WHEN** a user without instructor, manager, or admin role attempts to view another learner's profile
- **THEN** the system SHALL deny access

### Requirement: Achievement never alters academic or monetary records

The system SHALL keep competency attainment separate from grades, credits, graduation eligibility, certificates, and payments unless a separate capability explicitly consumes it.

#### Scenario: Competency corrected
- **WHEN** an Admin corrects or revokes an evidence-based attainment
- **THEN** no grade, credit, certificate, or payment record changes
