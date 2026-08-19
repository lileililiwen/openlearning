# review-followups Specification

## Purpose
TBD - created by archiving change review-followups. Update Purpose after archive.
## Requirements
### Requirement: Users can comment on reviews

The system SHALL allow enrolled Students and the course owner to add follow-up comments under a course review.

#### Scenario: Comment on review
- **WHEN** an enrolled Student or the course owner posts a comment on a review
- **THEN** the comment is shown under that review

#### Scenario: Instructor comment flagged
- **WHEN** the course owner comments on a review
- **THEN** the comment is marked as from the Instructor

#### Scenario: Non-enrolled cannot comment
- **WHEN** a user who is not enrolled and not the owner attempts to comment
- **THEN** the comment is rejected

### Requirement: Review comments can be moderated

The system SHALL allow an Admin (and the author) to remove a review comment.

#### Scenario: Remove comment
- **WHEN** an Admin or the comment author removes a comment
- **THEN** the comment is no longer shown

