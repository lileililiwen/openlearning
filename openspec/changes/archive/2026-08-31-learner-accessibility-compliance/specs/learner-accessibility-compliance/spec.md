# Learner Accessibility Compliance Specification

## ADDED Requirements

### Requirement: Document language matches content

The rendered page SHALL declare a document language that matches its content.

#### Scenario: English content is rendered

- **WHEN** a page renders English content
- **THEN** the document declares `lang="en"` (or the active culture's language)

### Requirement: Progress is exposed to assistive technology

Progress indicators SHALL expose their value to assistive technology.

#### Scenario: A progress bar is shown

- **WHEN** the UI renders a progress bar at 42%
- **THEN** it exposes the value 42 (via `aria-valuenow`/`aria-valuemin`/`aria-valuemax` or an equivalent text alternative) and a descriptive label

### Requirement: Keyboard users can skip to content

Pages SHALL provide a mechanism to bypass repetitive navigation.

#### Scenario: Keyboard user enters the page

- **WHEN** a keyboard user tabs into the page
- **THEN** the first focusable element is a skip link that moves focus to the main content

### Requirement: Mobile navigation is keyboard operable

The mobile navigation SHALL be operable without a pointer.

#### Scenario: Opening the mobile menu

- **WHEN** a user opens the mobile navigation
- **THEN** the toggle exposes its state (`aria-expanded`), focus is moved into the menu, `Esc` closes it and returns focus to the toggle

### Requirement: Form controls are labelled

Every form control SHALL have an associated label or equivalent.

#### Scenario: An input is rendered

- **WHEN** a page renders a text input such as a study note
- **THEN** that input has a programmatically associated label or `aria-label`
