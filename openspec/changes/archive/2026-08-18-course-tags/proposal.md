## Why

The catalog supports categories, search, filters, and sorting but not tags. Tags are a lighter-weight, multi-valued classification that the reference system lists under Core Course (Course List: "tags").

## What Changes

- Instructors can tag a course with zero or more tags during create/edit.
- Catalog filtering by tag (alongside category); course cards and details show tags.
- Admin can maintain the tag vocabulary (create/rename/retire tags) — see `category-tag-admin` for the admin surface; this change covers the data model and student-facing display/filter.

## Capabilities

### New Capabilities
- `course-tags`: multi-valued course tags with catalog filtering and display.

### Modified Capabilities

- `course-management`: course create/edit gains a tags field.
- `course-discovery`: catalog gains tag filtering and tag badges.

## Impact

- `Course` gains a `CourseTag` join (Course ↔ Tag); `Tag { Id, Name, Slug, IsActive }`.
- `CourseService` create/update accept tag names; `SearchAsync` gains a `tag` filter; `TagService` (list active, ensure by name).
- Course form renders a tag multi-select; catalog adds a tag filter + badges.
