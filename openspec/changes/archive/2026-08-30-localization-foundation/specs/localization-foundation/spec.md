# Localization Foundation Specification

## ADDED Requirements

### Requirement: User-facing strings are externalized

User-facing strings SHALL be resolved through a localization mechanism rather than hardcoded in markup.

#### Scenario: A page renders a user-facing string

- **WHEN** a learner-facing page renders a label or message
- **THEN** the string is resolved via `IStringLocalizer` from a resource, not hardcoded

### Requirement: Language can be selected and applied

The application SHALL support at least English and Chinese and apply the selected culture to the response.

#### Scenario: A user requests Chinese

- **WHEN** a request selects the `zh` culture (query, cookie, or accept-language)
- **THEN** the rendered strings use the Chinese resource and the document language reflects `zh`

### Requirement: A page does not mix languages

No rendered page SHALL mix Chinese and English user-facing strings within the same view.

#### Scenario: A previously mixed page is rendered

- **GIVEN** a page that previously showed mixed Chinese/English text
- **WHEN** it is rendered after migration
- **THEN** all user-facing strings belong to the active culture
