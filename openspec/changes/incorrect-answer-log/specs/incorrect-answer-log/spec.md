## ADDED Requirements

### Requirement: Wrong answers are collected per student

The system SHALL record every incorrect answer from a quiz or exam attempt into the Student's incorrect-answer log.

#### Scenario: Log wrong answer
- **WHEN** a Student answers a question incorrectly in a quiz or exam
- **THEN** the question, chosen answer, and correct answer are recorded in their log

#### Scenario: No duplicate active entry
- **WHEN** the same question is answered incorrectly again from the same source
- **THEN** the existing active entry is not duplicated

### Requirement: Student can review and practice logged questions

The system SHALL let a Student view their incorrect-answer log and take a practice quiz built from those questions.

#### Scenario: View log
- **WHEN** a Student opens the practice page
- **THEN** their incorrect answers are listed with correct answers and bookmark state

#### Scenario: Practice resolves
- **WHEN** a Student answers a logged question correctly in practice
- **THEN** the entry is marked resolved and no longer shown in the active log

### Requirement: Student can bookmark questions

The system SHALL allow a Student to bookmark questions from their log for later review.

#### Scenario: Bookmark toggle
- **WHEN** a Student bookmarks or unbookmarks a question
- **THEN** the bookmark state is saved and reflected in the log filters
