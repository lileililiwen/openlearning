# Learner Experience Quality Specification

## ADDED Requirements

### Requirement: Learner navigation preserves context

The learner shell SHALL show the current course and location, expose a consistent route back to the learner dashboard, and identify the primary next learning action.

#### Scenario: Learner moves between lessons

- **WHEN** a learner navigates between lessons in a course
- **THEN** course context, progress, current location, and a next-action affordance remain available

### Requirement: UI states are explicit and recoverable

Learner pages SHALL define loading, empty, error, success, and disabled states, and recoverable errors MUST provide a retry action without silently discarding safe user input.

#### Scenario: Course list fails

- **WHEN** the course list cannot be loaded
- **THEN** the page explains the failure, provides retry, and does not render a misleading empty catalog

#### Scenario: No enrollments exist

- **WHEN** a learner has no enrollments
- **THEN** the dashboard explains the empty state and provides a catalog action

### Requirement: Learner workflows meet accessibility behavior

Learner workflows SHALL provide semantic landmarks, labelled controls, visible focus, keyboard operation, logical focus order, and WCAG 2.2 AA contrast for supported themes.

#### Scenario: Keyboard-only lesson completion

- **WHEN** a learner uses only the keyboard to complete a lesson
- **THEN** all controls are reachable, focus is visible, and completion can be submitted without pointer input

#### Scenario: Narrow viewport

- **WHEN** a learner uses a narrow viewport
- **THEN** content remains readable and actions remain usable without horizontal scrolling for ordinary page content
