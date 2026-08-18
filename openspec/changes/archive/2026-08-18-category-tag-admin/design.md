# Category & Tag Admin — Design

## Context

Categories are free text; tags auto-create. Without admin control the vocabulary fragments.

## Goals

- Admins maintain the category list; courses pick from it.
- Admins maintain the tag list with rename/merge/retire.
- Filters and forms use the managed vocabularies.

## Non-Goals

- No multi-level category trees (flat, ordered list).
- No localized names.
- No per-course custom categories beyond admin-defined ones.

## Decisions

### D1: Managed categories
`Category { Id, Name, Slug, OrderIndex, IsActive }` in `OpenLearning.CourseManagement`. To avoid rewriting every course, `Course.Category` remains a string that is kept in sync: when a category is renamed, update all matching `Course.Category` values in one query. Course forms render a dropdown from active categories (with the existing free-text input removed). Catalog `GetCategoriesAsync` reads from `Category` (active only).

### D2: Tag admin operations
`TagService` gains `RenameAsync(oldName,newName)` (updates all `CourseTag` joins by re-pointing), `MergeAsync(fromTagId,toTagId)` (re-point joins, delete from), `RetireAsync(tagId)` (set `IsActive=false`, hidden from filters/forms). Courses keep auto-creating unknown tags unless `RequireAdminTags` config is on — decision: auto-create remains, admin can merge/retire to control the vocabulary.

### D3: Admin UI
`/Admin/Categories`: list, create, edit (rename cascades), deactivate. `/Admin/Tags`: list with course counts, rename, merge (choose target), retire.

## Risks / Trade-offs

- **Rename cascade** → Single UPDATE on `Courses.Category` (or tag joins) keeps integrity; slugs unchanged on rename.
- **Free-text removal** → Existing courses with orphan categories keep their text; an admin can create a matching category to adopt them.

## Migration Plan

One migration creates `Categories`; no schema change for tags.

## Open Questions

- Should category selection be mandatory? MVP: optional, default "Uncategorized".
