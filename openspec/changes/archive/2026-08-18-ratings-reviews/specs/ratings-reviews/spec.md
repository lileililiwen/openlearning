## ADDED Requirements

### Requirement: Enrolled student can rate and review a course

The system SHALL allow an enrolled Student to rate a course from 1 to 5 and optionally write a review comment, with one review per student per course.

#### Scenario: Submit a rating
- **WHEN** an enrolled Student submits a rating for a course
- **THEN** the rating is stored for that Student and course

#### Scenario: One review per course
- **WHEN** an enrolled Student submits another review for the same course
- **THEN** the previous review is replaced

#### Scenario: Non-enrolled student cannot rate
- **WHEN** a Student who is not enrolled attempts to rate a course
- **THEN** the system SHALL deny the request

### Requirement: Courses show aggregate rating

The system SHALL display each course's average rating and rating count on catalog cards and the course detail page.

#### Scenario: Aggregate display
- **WHEN** a visitor views a course card or detail page
- **THEN** the average rating and count are shown when reviews exist

### Requirement: Owner views reviews and admin moderates

The system SHALL allow the course owner to view their course's reviews and SHALL allow an Admin to remove any review.

#### Scenario: Owner views reviews
- **WHEN** the owning Instructor opens a course's reviews
- **THEN** all reviews with author and rating are shown

#### Scenario: Admin removes a review
- **WHEN** an Admin removes an inappropriate review
- **THEN** the review is deleted and no longer counted in the aggregate
