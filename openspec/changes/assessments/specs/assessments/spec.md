## ADDED Requirements

### Requirement: Instructor can create and manage quizzes

The system SHALL allow the Instructor who owns a course to create, edit, and delete quizzes for that course, each with a title and description.

#### Scenario: Owner creates a quiz
- **WHEN** an Instructor who owns the course submits a new quiz with a title and description
- **THEN** a quiz is created for that course and appears in the course's quiz list

#### Scenario: Owner edits a quiz
- **WHEN** the owning Instructor saves changes to a quiz's title and description
- **THEN** the quiz metadata is updated

#### Scenario: Non-owner edit is denied
- **WHEN** an Instructor who does not own the course attempts to edit or delete its quiz
- **THEN** the system SHALL deny access

### Requirement: Quiz contains ordered multiple-choice questions

The system SHALL allow a quiz owner to add, edit, and delete questions to a quiz. Each question SHALL have text, a point value, a display order, and 2-4 answer options with exactly one marked correct.

#### Scenario: Add question with options
- **WHEN** an owner adds a question with text, points, and at least two answer options to a quiz
- **THEN** the question is created at the next order position and appears in the quiz

#### Scenario: Correct answer required
- **WHEN** an owner saves a question without exactly one correct answer option
- **THEN** the system SHALL reject the question with a validation error

#### Scenario: Delete question
- **WHEN** an owner deletes a question from a quiz
- **THEN** the question and its answer options are removed

### Requirement: Enrolled student can take a quiz and receive a score

The system SHALL allow an enrolled Student to submit answers for a quiz in a course they are enrolled in, and SHALL store the attempt with a computed score.

#### Scenario: Submit quiz answers
- **WHEN** an enrolled Student submits an answer for every question in a quiz
- **THEN** an attempt is recorded with a score equal to the sum of points of correctly answered questions
- **THEN** the student is shown the score and a per-question correct/incorrect breakdown

#### Scenario: Non-enrolled student cannot take quiz
- **WHEN** a Student who is not enrolled in the course attempts to submit answers for a quiz
- **THEN** the system SHALL deny the request

### Requirement: Attempt results are visible to owner and student

The system SHALL allow the quiz owner to view all attempts for a quiz and SHALL allow a Student to view their own attempts.

#### Scenario: Student views own result
- **WHEN** a Student opens a quiz they have attempted
- **THEN** the system shows their stored score and breakdown

#### Scenario: Instructor views all attempts
- **WHEN** the owning Instructor opens a quiz's results
- **THEN** the system shows every attempt with student, date, score, and maximum score
