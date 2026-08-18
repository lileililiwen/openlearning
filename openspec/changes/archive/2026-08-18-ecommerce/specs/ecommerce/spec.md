## ADDED Requirements

### Requirement: Instructor can set a course price

The system SHALL allow the Instructor who owns a course to set an optional price for it. A course with no price or a price of zero is free; a priced course requires purchase before enrollment.

#### Scenario: Owner sets a price
- **WHEN** the owning Instructor saves a course with a price greater than zero
- **THEN** the course is priced and its catalog/detail views display the price

#### Scenario: Free course
- **WHEN** the owning Instructor leaves the price empty or zero
- **THEN** the course remains free and students can enroll directly

### Requirement: Student can purchase a paid course

The system SHALL allow a Student to purchase a published paid course through a checkout flow, and SHALL grant enrollment once payment is confirmed.

#### Scenario: Buy a paid course
- **WHEN** a Student opens the checkout for a paid course and confirms payment
- **THEN** a paid order is recorded for that Student and course
- **THEN** the Student becomes enrolled in the course

#### Scenario: Paid course cannot be enrolled directly
- **WHEN** a Student attempts to enroll directly in a paid course without a paid order
- **THEN** the system SHALL reject the enrollment request

#### Scenario: Duplicate purchase is prevented
- **WHEN** a Student who is already enrolled attempts to buy the same course again
- **THEN** the system SHALL reject the purchase

### Requirement: Instructor can view course orders

The system SHALL allow the course owner to view orders for their course, including the buyer, amount, and payment status.

#### Scenario: Owner views orders
- **WHEN** the owning Instructor opens a course's order list
- **THEN** the system shows each order with student, amount, and status
