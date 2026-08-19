# assignments Specification

## Purpose
TBD - created by archiving change assignments. Update Purpose after archive.
## Requirements
### Requirement: Instructor can create assignments

The system SHALL allow the course owner to create, edit, and delete assignments with instructions and an optional due date.

#### Scenario: Create assignment
- **WHEN** the owning Instructor creates an assignment for a course
- **THEN** enrolled students can see and submit to it

#### Scenario: Non-owner cannot manage
- **WHEN** an Instructor who does not own the course attempts to create or edit an assignment
- **THEN** the system SHALL deny access

### Requirement: Student can submit an assignment

The system SHALL allow an enrolled Student to submit an assignment with text and/or an uploaded file, and to resubmit according to the assignment's policy.

#### Scenario: Submit
- **WHEN** an enrolled Student submits an assignment
- **THEN** the submission is stored for that student and assignment

#### Scenario: Resubmit before grading
- **WHEN** an enrolled Student submits again before the instructor has graded
- **THEN** the previous submission is replaced

#### Scenario: Resubmit after grading
- **WHEN** an assignment does not allow resubmission after grading and the Student submits after being graded
- **THEN** the submission is rejected

### Requirement: Instructor grades submissions

The system SHALL allow the owning Instructor to grade a submission with a score and written feedback, which the Student can view.

#### Scenario: Grade
- **WHEN** an Instructor grades a submission
- **THEN** the score and feedback are saved and shown to the student

#### Scenario: View feedback
- **WHEN** a Student opens a graded assignment
- **THEN** their score and the instructor's feedback are shown

