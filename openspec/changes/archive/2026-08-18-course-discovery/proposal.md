## Why

The catalog is a static list. Students cannot search, filter, or sort courses, and course cards lack the metadata (level, duration, rating) that drives buying/learning decisions.

## What Changes

- **Course metadata**: instructors can set level (beginner/intermediate/advanced), estimated duration, language, prerequisites, and learning outcomes on a course.
- **Search & discovery**: the catalog gains full-text search over title/description/category, category filtering, sorting (newest, popular, price, rating), and pagination.
- Course cards and the detail page display the new metadata and aggregated rating (rating display arrives with `ratings-reviews`; cards degrade gracefully until then).

## Capabilities

### New Capabilities
- `course-discovery`: full-text search, category filter, sort, pagination, and metadata-driven course cards.

### Modified Capabilities
- `course-management`: course create/edit now also captures level, duration, language, prerequisites, and outcomes.

## Impact

- `Course` entity gains metadata columns (level enum, duration string, language string, prerequisites, outcomes) — one migration.
- Catalog page (`Pages/Index`) becomes a search/filter/sort view; a query helper on `CourseService` builds the filtered query.
- No changes to enrollment/progress behavior.
