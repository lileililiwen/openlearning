# Course Discovery — Tasks

## 1. Course Metadata

- [ ] 1.1 Add `Level` (enum), `Duration`, `Language`, `Prerequisites`, `LearningOutcomes` to `Course` + EF config
- [ ] 1.2 Add metadata fields to course create/edit forms (owner-only)
- [ ] 1.3 Create EF Core migration

## 2. Search & Discovery

- [ ] 2.1 Implement `CourseService.SearchAsync(search, category, sort, page, pageSize)` with total count
- [ ] 2.2 Rebuild the catalog page as a search/filter/sort/paginated view
- [ ] 2.3 Show metadata (level, duration, language, price/free) on cards and course detail

## 3. Verification

- [ ] 3.1 Run `dotnet build` and start the app
- [ ] 3.2 Verify search, filter, sort, pagination, and metadata display
