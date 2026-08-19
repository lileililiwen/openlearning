# Lesson Preview — Tasks

## 1. Data

- [x] 1.1 Add `IsPreview` to `Lesson` + config
- [x] 1.2 Extend `LessonService.CreateAsync`/`UpdateAsync` to persist the flag

## 2. UI

- [x] 2.1 Preview checkbox on lesson create/edit forms
- [x] 2.2 Course details: preview badge + links for non-enrolled visitors; enrolled view unchanged
- [x] 2.3 `View` page: allow published preview lessons for non-enrolled; skip progress recording for them

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: non-enrolled sees + opens preview, non-preview stays gated, no progress recorded for preview viewers, draft course still gated
