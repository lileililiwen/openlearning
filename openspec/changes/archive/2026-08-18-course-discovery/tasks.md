# Course Discovery — Tasks

## 1. Course Metadata

- [x] 1.1 Add `Level` (enum), `Duration`, `Language`, `Prerequisites`, `LearningOutcomes` to `Course` + EF config
- [x] 1.2 Add metadata fields to course create/edit forms (owner-only)
- [x] 1.3 Create EF Core migration

## 2. Search & Discovery

- [x] 2.1 Implement `CourseService.SearchAsync(search, category, sort, page, pageSize)` with total count
- [x] 2.2 Rebuild the catalog page as a search/filter/sort/paginated view
- [x] 2.3 Show metadata (level, duration, language, price/free) on cards and course detail

## 3. Verification

- [x] 3.1 Run `dotnet build` and start the app
- [x] 3.2 Verify search, filter, sort, pagination, and metadata display
