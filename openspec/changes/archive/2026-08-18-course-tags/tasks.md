# Course Tags — Tasks

## 1. Data & Service

- [x] 1.1 Add `Tag` + `CourseTag` entities + configs (unique slug, composite join key)
- [x] 1.2 Add `Tags` collection to `Course` and `TagService` (list active, ensure by name)
- [x] 1.3 Extend `CourseService.CreateAsync`/`UpdateAsync` to accept tag names; `SearchAsync` gains `tag` filter
- [x] 1.4 Register assembly scanning + `TagService`

## 2. UI

- [x] 2.1 Tag multi-select (or comma input) on course create/edit
- [x] 2.2 Catalog tag filter + tag badges on cards and course details

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: tagging a course, filtering by tag, badges render, unknown tags created on save
