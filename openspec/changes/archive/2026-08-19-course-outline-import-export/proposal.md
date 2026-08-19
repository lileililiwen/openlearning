## Why

Instructors often have an offline curriculum outline (Word / Excel) they want to upload as a starting point for a course. Manual entry of dozens of modules and hundreds of lessons is also a deal-breaker. The brief explicitly limits this to **metadata only** — video / files still go through the upload endpoints because binary content cannot be reliably imported via Excel.

## What Changes

- Provide an Excel import/export for course metadata: `Course` (limited fields) → `Module` (title, order) → `Lesson` (title, order, optional content URL text reference).
- Importing a course outline does NOT import media; the lesson content URL is a text reference and is verified to point at an existing file (if the URL is recognised) or kept as-is for the instructor to attach the file later.
- Sync ≤200 rows; async (via `async-io-jobs`) for larger outlines.
- Two modes: `Append` (new modules/lessons only) and `Replace` (wipe existing outline under the course and re-import — gated by an explicit confirmation and an audit-log entry).
- Ownership: only the course owner can import; Admin can import into any course.

## Capabilities

### New Capabilities

- `course-outline-import-export`: Excel import/export of course modules and lessons; metadata-only.

### Modified Capabilities

- `course-structure`: `ModuleService` and `LessonService` gain bulk-creation methods used by the import.
- `course-management`: the course edit page exposes an "Import outline" link.

## Impact

- New `OpenLearning.CourseOutlineIO` module: `OutlineImportJob { Id, UserId, CourseId, Mode (Append/Replace), FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }`, `OutlineRowError { Id, JobId, RowIndex, Field, Message }`. EF migration `AddCourseOutlineIO`.
- Services: `OutlineImportService.ImportSyncAsync`, `OutlineImportService.ImportAsync`, `OutlineImportService.ProcessJobAsync`, `OutlineExportService.ExportAsync`.
- Pages: `Pages/Courses/Outline/Import.cshtml(.cs)`, `Pages/Courses/Outline/Export.cshtml(.cs)`.
- One-line DI: `builder.Services.AddCourseOutlineIOModule();`.