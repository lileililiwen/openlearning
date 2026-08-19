## Why

Instructors authoring quizzes, exams, and assignments need to bring in hundreds-to-thousands of questions from existing Word/Excel repositories; one-by-one manual entry is a deal-breaker. The platform already supports multi-type questions via the pending `question-types` change (single/multiple/true-false/fill-blank/short answer/file upload) and a bank via `question-bank-admin`. It lacks bulk Excel import/export with partial-success semantics and the role isolation the brief calls out (an instructor must only see their own questions, never the platform-wide bank).

## What Changes

- Provide an Excel import/export surface for `Question` rows, gated to the course owner (questions per course / per quiz) and to instructors for their own authored questions.
- Sync path for small imports (≤200 rows) returns row-by-row errors on the same request; large imports (>200 rows) go through the `async-io-jobs` framework, with a notification when the file is ready.
- Partial-success mode: correct rows commit; error rows are written to a downloadable error file.
- Two import modes: `Append` (new questions only) and `UpdateOrAppend` (rows with a known `QuestionId` update; rows without create).
- Export: filterable by question type / difficulty / knowledge tag, outputs `.xlsx`, streams via SXSSF (no full memory load).

## Capabilities

### New Capabilities

- `question-import-export`: Excel template, sync + async paths, partial-success reporting, append/update modes, role-scoped access.

### Modified Capabilities

- `assessments`: question model gains an optional external `RowId` column for stable re-imports.
- `question-types` (pending): import resolves the `QuestionType` enum; rows with an unknown type are reported as errors rather than coerced.
- `question-bank-admin` (pending): bank questions use the same import path with `IsBank = true`.
- `notification-events-extensions` (proposed): receives new `import.completed`, `import.failed`, `export.ready` events.

## Impact

- New `OpenLearning.QuestionIO` module: `QuestionImportJob { Id, UserId, CourseId?, QuizId?, Mode (Append/UpdateOrAppend), FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }`, `QuestionRowError { Id, JobId, RowIndex, Field, Message }`. EF migration `AddQuestionIO`.
- Services: `QuestionImportService` (parse → validate → persist), `QuestionExportService` (streaming via ClosedXML SXSSF or EPPlus).
- Pages: `Pages/Courses/Quizzes/Import.cshtml(.cs)` (sync ≤200), `Pages/Courses/Quizzes/ImportAsync.cshtml(.cs)` (submit >200), `Pages/Courses/Quizzes/Export.cshtml(.cs)`; question bank equivalents under `/Admin/QuestionBank/Import` and `/Export`.
- File storage uses existing `OpenLearning.Storage` for uploads and result files; retention is handled by `scheduled-business-jobs`.
- One-line DI: `builder.Services.AddQuestionIOModule();`.