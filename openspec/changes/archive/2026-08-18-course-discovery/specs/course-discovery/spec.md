## ADDED Requirements

### Requirement: Student can search the catalog

The system SHALL allow a visitor to search published courses by keyword across title, description, and category.

#### Scenario: Keyword search
- **WHEN** a visitor enters a search term on the catalog
- **THEN** matching published courses are shown

#### Scenario: No matches
- **WHEN** a search returns no courses
- **THEN** the system SHALL show an empty state

### Requirement: Student can filter and sort the catalog

The system SHALL allow filtering published courses by category and sorting by newest, popular (enrollments), price, or rating, with pagination.

#### Scenario: Filter by category
- **WHEN** a visitor selects a category filter
- **THEN** only courses in that category are shown

#### Scenario: Sort and paginate
- **WHEN** a visitor picks a sort order and navigates pages
- **THEN** the results are ordered accordingly and paginated

### Requirement: Course cards show metadata

The system SHALL display level, duration, language, rating, and price on catalog cards and the course detail page.

#### Scenario: Card metadata
- **WHEN** a visitor views a course card
- **THEN** level, duration, rating (when available), and price/free are shown
