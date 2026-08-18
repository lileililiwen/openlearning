## ADDED Requirements

### Requirement: Admin can define membership plans

The system SHALL allow an Admin to create and manage membership plans with a price, description, and validity duration.

#### Scenario: Create plan
- **WHEN** an Admin creates a membership plan with a price and duration
- **THEN** the plan is listed on a public membership page and can be purchased

#### Scenario: Deactivate plan
- **WHEN** an Admin deactivates a plan
- **THEN** the plan is no longer purchasable but existing memberships remain valid

### Requirement: Student can purchase and renew membership

The system SHALL allow a Student to purchase an active plan, creating a membership with a start and expiry date, and to renew before expiry.

#### Scenario: Purchase membership
- **WHEN** a Student purchases a membership plan
- **THEN** an active membership is created with the plan's duration

#### Scenario: Renew membership
- **WHEN** a Student renews their active membership
- **THEN** the expiry date is extended by the plan's duration

### Requirement: Active members receive plan benefits

The system SHALL grant active members free enrollment in paid courses.

#### Scenario: Member enrolls in a paid course
- **WHEN** a Student with an active membership enrolls in a paid course
- **THEN** the course is enrolled without purchase

#### Scenario: Expired membership
- **WHEN** a Student's membership has expired
- **THEN** the member benefit no longer applies and the course must be purchased

### Requirement: Expiry reminders are sent

The system SHALL notify a Student whose membership expires within 7 days.

#### Scenario: Reminder notification
- **WHEN** an active membership is within 7 days of expiry
- **THEN** the Student receives a notification that the membership is expiring
