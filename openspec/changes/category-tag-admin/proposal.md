## Why

Categories are free-text on courses and tags (from `course-tags`) are auto-created. The reference system's Admin Backend requires Category & Tag Management: a controlled vocabulary that admins maintain.

## What Changes

- Category management: admins define categories; course forms select from them (renaming merges courses).
- Tag management: admins create, rename, merge, and retire tags.
- Course list filters use the managed vocabularies.

## Capabilities

### New Capabilities
- `category-tag-admin`: admin-managed course categories and tag vocabulary.

### Modified Capabilities

- `course-management`: course form selects a managed category.
- `course-tags`: tag vocabulary gains admin CRUD (rename/merge/retire).

## Impact

- New `Category { Id, Name, Slug, OrderIndex, IsActive }` in `OpenLearning.CourseManagement`; `Course.Category` becomes a FK (or retains text synced to the category name).
- `CategoryService` (CRUD admin, list active) and `TagService` gains admin operations (rename/merge/retire).
- Admin pages `/Admin/Categories` and `/Admin/Tags`.
