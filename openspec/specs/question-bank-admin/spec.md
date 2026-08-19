# question-bank-admin Specification

## Purpose
TBD - created by archiving change question-bank-admin. Update Purpose after archive.
## Requirements
### Requirement: Admin maintains a central question bank

The system SHALL allow an Admin to create, edit, archive, and search questions in a central bank.

#### Scenario: Create bank question
- **WHEN** an Admin creates a bank question with a topic
- **THEN** the question appears in bank search results

#### Scenario: Archive bank question
- **WHEN** an Admin archives a bank question
- **THEN** it no longer appears in active search results but remains on quizzes that imported it

#### Scenario: Search
- **WHEN** an Admin searches the bank by topic or text
- **THEN** matching bank questions are listed

### Requirement: Instructors import bank questions

The system SHALL allow an Instructor to import bank questions into their own quiz or exam as independent copies.

#### Scenario: Import
- **WHEN** an Instructor imports a bank question into their quiz
- **THEN** a copy is added to the quiz

#### Scenario: Snapshot independence
- **WHEN** the bank question is later edited or archived
- **THEN** the imported copy in the quiz is unchanged

#### Scenario: Ownership gating
- **WHEN** an Instructor tries to import into a quiz they do not own
- **THEN** the import is denied

