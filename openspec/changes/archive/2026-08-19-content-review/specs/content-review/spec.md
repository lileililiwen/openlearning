## ADDED Requirements

### Requirement: Courses pass through admin review

The system SHALL route new course publications through an Admin review step before the course becomes publicly published.

#### Scenario: Publish for review
- **WHEN** an Instructor publishes a course that is not already published
- **THEN** the course enters an under-review state and is not visible to students

#### Scenario: Approve
- **WHEN** an Admin approves a pending course
- **THEN** the course becomes published and visible

#### Scenario: Reject
- **WHEN** an Admin rejects a pending course with a note
- **THEN** the course returns to draft and the Instructor sees the note

### Requirement: Users can report content and admins resolve reports

The system SHALL let a signed-in user report a review, comment, question, post, or reply, and SHALL let an Admin remove or dismiss the reported content.

#### Scenario: Report content
- **WHEN** a user reports content they do not own
- **THEN** an open report appears in the Admin queue

#### Scenario: Remove content
- **WHEN** an Admin removes reported content
- **THEN** the content is hidden from all users and the report is resolved

#### Scenario: Dismiss report
- **WHEN** an Admin dismisses a report
- **THEN** the report is closed and the content stays visible
