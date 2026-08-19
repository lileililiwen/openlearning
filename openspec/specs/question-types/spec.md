# question-types Specification

## Purpose
TBD - created by archiving change question-types. Update Purpose after archive.
## Requirements
### Requirement: Quizzes support multiple question types

The system SHALL support true/false, fill-in-the-blank, short answer, and file-upload questions in addition to single- and multiple-choice.

#### Scenario: Create typed questions
- **WHEN** an Instructor creates a question with a supported type
- **THEN** the quiz-take page renders the matching input for that type

#### Scenario: Take typed questions
- **WHEN** a Student answers a typed question
- **THEN** the answer is stored in the shape appropriate to the type

### Requirement: Objective questions auto-score

The system SHALL auto-score single-choice, multiple-choice, true/false, and fill-in-the-blank questions.

#### Scenario: Auto score
- **WHEN** a quiz with objective questions is submitted
- **THEN** each objective answer is scored and the attempt percent reflects them

#### Scenario: Fill-blank matching
- **WHEN** a fill-in-the-blank answer is compared
- **THEN** it matches ignoring leading/trailing whitespace and case

### Requirement: Manual questions are graded by the instructor

The system SHALL mark short-answer and file-upload answers as pending and SHALL let the Instructor grade them with a score and feedback.

#### Scenario: Pending grading
- **WHEN** a quiz containing manual questions is submitted
- **THEN** those answers show as pending grading and are excluded from the auto score

#### Scenario: Grade answer
- **WHEN** an Instructor grades a pending answer with a score
- **THEN** the attempt's total score and percent are recalculated

