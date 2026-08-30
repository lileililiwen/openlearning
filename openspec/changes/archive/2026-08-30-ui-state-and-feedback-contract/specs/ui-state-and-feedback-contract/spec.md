# UI State & Feedback Contract Specification

## ADDED Requirements

### Requirement: Shared state primitives exist

Learner and instructor pages SHALL use shared, consistent components for loading, empty, error, success, confirmation, and disabled states rather than bespoke per-page markup.

#### Scenario: A page needs an empty state

- **WHEN** a page has no data to show
- **THEN** it renders the shared empty-state component with a clear message and an optional next action

#### Scenario: A page is loading

- **WHEN** content is being fetched
- **THEN** a shared loading indicator is shown and marked busy for assistive technology

### Requirement: User actions produce visible feedback

Every successful or failed action SHALL be communicated to the user through a visible message.

#### Scenario: An action sets a result message

- **WHEN** any handler sets `TempData["Message"]`
- **THEN** the message is displayed to the user (via a global toast) rather than silently discarded

#### Scenario: An action fails

- **WHEN** a submitted action fails validation or a transient error
- **THEN** an error state explains what failed and offers retry

### Requirement: Destructive actions are confirmed

Destructive actions SHALL require explicit confirmation before they take effect.

#### Scenario: Learner unpublishes a course

- **WHEN** an instructor submits "Unpublish"
- **THEN** they are asked to confirm before the change is applied

### Requirement: Recoverable failures preserve input

When a form submission fails, the system SHALL retain safe user input so it does not have to be re-entered.

#### Scenario: Note save fails

- **WHEN** a learner saves a study note and the request fails
- **THEN** the note text remains in the field for correction and resubmission

### Requirement: Common in-lesson actions work without full reloads

Frequent in-lesson actions SHALL update via progressive enhancement while remaining functional without JavaScript.

#### Scenario: Learner marks a lesson complete with JS disabled

- **WHEN** a learner submits "Mark as completed" without JavaScript
- **THEN** the action still completes via a standard form postback

#### Scenario: Learner marks a lesson complete with JS enabled

- **WHEN** a learner submits "Mark as completed" with JavaScript
- **THEN** the action updates the page region and shows feedback without reloading the video
