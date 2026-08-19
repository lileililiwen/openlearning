# course-outline-import-export Specification

## Purpose
TBD - created by archiving change course-outline-import-export. Update Purpose after archive.
## Requirements
### Requirement: Excel template for course outline

The system SHALL provide a downloadable Excel template with columns `ModuleTitle, ModuleOrder, LessonTitle, LessonOrder, LessonContentUrl (optional)`.

#### Scenario: Download template

- **WHEN** the course owner opens the import page
- **THEN** a `.xlsx` template is returned with the headers and a sample row

### Requirement: Sync import for small outlines

The system SHALL accept uploaded `.xlsx` files of up to 200 valid rows synchronously and return row-by-row errors.

#### Scenario: Sync success

- **WHEN** an Instructor uploads 100 valid rows
- **THEN** the modules and lessons are created and the response shows success counts

#### Scenario: Partial success

- **WHEN** 100 rows are uploaded of which 7 are invalid
- **THEN** the valid rows are persisted and the 7 errors are reported with row numbers and reasons

### Requirement: Async import for large outlines

The system SHALL route uploads with more than 200 valid rows through `async-io-jobs`, returning a job id; the result is delivered via `import.completed` / `import.failed` notifications.

#### Scenario: Submit async job

- **WHEN** an Instructor uploads a 1500-row outline
- **THEN** the request returns a job id and the page shows "任务已提交，完成后将通过站内信通知"

### Requirement: Append and Replace modes

The system SHALL support two import modes: `Append` (only new modules/lessons) and `Replace` (wipe the course's modules and lessons, then re-import).

#### Scenario: Append default

- **WHEN** the Instructor submits without specifying a mode
- **THEN** the default mode is `Append`

#### Scenario: Replace with confirmation

- **WHEN** the Instructor selects `Replace`
- **THEN** the page requires an explicit confirmation before submission and the operation log records the wipe

#### Scenario: Replace wipes only modules and lessons

- **WHEN** the Replace import runs
- **THEN** the course's modules and lessons are deleted; the course row itself, enrollments, quizzes, and assignments are preserved

### Requirement: Metadata only — no media import

The system SHALL NOT import media files via Excel. The `LessonContentUrl` column is accepted as a text reference; the lesson itself remains a placeholder until the Instructor attaches media via the lesson edit page.

#### Scenario: Lesson created without media

- **WHEN** a row contains only `LessonTitle` and `LessonOrder`
- **THEN** the lesson is created with no media attached and the course detail page shows a "media not attached" placeholder

#### Scenario: LessonContentUrl as reference

- **WHEN** a row supplies `LessonContentUrl = "https://example.com/lecture.mp4"`
- **THEN** the lesson stores the URL as a text reference; the player still requires the Instructor to attach a managed file before playback works (or the URL is recognised as already on file)

### Requirement: Ownership isolation

The system SHALL ensure the importing Instructor owns the course; Admin can import into any course; TAs cannot.

#### Scenario: Owner can import

- **WHEN** the course owner uploads
- **THEN** the request is accepted

#### Scenario: Admin can import

- **WHEN** an Admin uploads
- **THEN** the request is accepted

#### Scenario: TA denied

- **WHEN** a TA attempts the import
- **THEN** access is denied

#### Scenario: Non-owner instructor denied

- **WHEN** an Instructor who does not own the course attempts to import
- **THEN** access is denied

### Requirement: Streaming export

The system SHALL export a course's outline to `.xlsx` using SXSSF streaming; modules and lessons preserve order and titles.

#### Scenario: Owner exports

- **WHEN** the owner opens `/Courses/{id}/Outline/Export`
- **THEN** a `.xlsx` is downloaded with one row per lesson (carrying the parent module's title and order)

#### Scenario: Non-owner denied

- **WHEN** another Instructor opens the export endpoint
- **THEN** access is denied

### Requirement: Validation rules

The system SHALL validate each row: `ModuleTitle` required, `LessonTitle` required when the row represents a lesson, `ModuleOrder` and `LessonOrder` non-negative integers, no two rows with the same `(ModuleOrder, LessonOrder)`.

#### Scenario: Missing title

- **WHEN** a row's `ModuleTitle` is empty
- **THEN** the row is reported as an error

#### Scenario: Duplicate order

- **WHEN** two rows share the same `(ModuleOrder, LessonOrder)` within the same module
- **THEN** the second row is reported as `duplicate order`

### Requirement: File safety

The system SHALL accept only `.xlsx` files up to 5 MB; larger or differently-formatted files SHALL be rejected before parsing.

#### Scenario: Oversize rejected

- **WHEN** an Instructor uploads a 7 MB `.xlsx`
- **THEN** the request is rejected with a 400

### Requirement: Audit log

The system SHALL write an operation-log entry per outline-import job recording the importer, mode, file key, and counts.

#### Scenario: Audit recorded

- **WHEN** an import job finishes
- **THEN** an entry is visible in `/Admin/Logs/Operations`

