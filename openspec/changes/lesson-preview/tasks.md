# Lesson Preview — Tasks

## 1. Data

- [ ] 1.1 Add `IsPreview` to `Lesson` + config
- [ ] 1.2 Extend `LessonService.CreateAsync`/`UpdateAsync` to persist the flag

## 2. UI

- [ ] 2.1 Preview checkbox on lesson create/edit forms
- [ ] 2.2 Course details: preview badge + links for non-enrolled visitors; enrolled view unchanged
- [ ] 2.3 `View` page: allow published preview lessons for non-enrolled; skip progress recording for them

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: non-enrolled sees + opens preview, non-preview stays gated, no progress recorded for preview viewers, draft course still gated
