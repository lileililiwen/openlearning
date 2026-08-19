# qa-community Specification

## Purpose
TBD - created by archiving change qa-community. Update Purpose after archive.
## Requirements
### Requirement: Enrolled students can use course Q&A

The system SHALL allow enrolled Students to ask questions in a course Q&A and to reply to questions, with answers from the Instructor marked as such.

#### Scenario: Ask a question
- **WHEN** an enrolled Student posts a question in a course
- **THEN** the question is listed in the course Q&A for other enrolled students and the instructor

#### Scenario: Reply
- **WHEN** an enrolled Student or the Instructor replies to a question
- **THEN** the reply is shown under the question

#### Scenario: Instructor answer badge
- **WHEN** the course owner replies to a question
- **THEN** the reply is marked as an Instructor answer

#### Scenario: Non-enrolled cannot read or write
- **WHEN** a non-enrolled visitor tries to open the course Q&A
- **THEN** access is denied

### Requirement: Course community posts

The system SHALL allow enrolled Students and the Instructor to post text in a class-group community and reply to posts, visible only within the course.

#### Scenario: Post and reply
- **WHEN** an enrolled user posts and another replies
- **THEN** both are shown in the course community

### Requirement: Admin can moderate community content

The system SHALL allow an Admin to remove any question, post, or reply.

#### Scenario: Admin removes content
- **WHEN** an Admin removes a question, post, or reply
- **THEN** it is no longer shown to anyone

