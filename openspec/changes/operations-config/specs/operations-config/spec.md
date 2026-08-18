## ADDED Requirements

### Requirement: Admin configures homepage banners

The system SHALL allow an Admin to create, order, activate, and deactivate carousel banners shown on the homepage.

#### Scenario: Add banner
- **WHEN** an Admin adds an active banner with an image and link
- **THEN** the banner is shown in the homepage carousel in its configured order

#### Scenario: Deactivate banner
- **WHEN** an Admin deactivates a banner
- **THEN** the banner no longer appears

### Requirement: Admin schedules pop-ups and campaigns

The system SHALL allow an Admin to schedule pop-ups with start/end dates and group banners under campaigns with their own windows.

#### Scenario: Active pop-up
- **WHEN** the current time is within an active pop-up's window
- **THEN** the pop-up is shown once per session

#### Scenario: Campaign window
- **WHEN** the current time is within a campaign's window
- **THEN** the campaign's banners are eligible for display

#### Scenario: Outside window
- **WHEN** the current time is outside the pop-up or campaign window
- **THEN** the content is not displayed
