## ADDED Requirements

### Requirement: Excel template for questions

The system SHALL provide a downloadable Excel template with columns `RowId(可选), QuestionType, Stem, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, Explanation, Difficulty, KnowledgeTag`. The `QuestionType` column accepts a fixed set of values; required columns are highlighted.

#### Scenario: Download template

- **WHEN** an Instructor opens the import page for a quiz they own
- **THEN** a `.xlsx` template is returned with the column headers and a sample row

#### Scenario: QuestionType whitelist

- **WHEN** an uploaded file contains a `QuestionType` value outside the supported set
- **THEN** the row is reported as an error with field=`QuestionType` and message listing the allowed values

### Requirement: Sync import for small files

The system SHALL accept uploaded `.xlsx` files of up to 200 valid rows synchronously and SHALL return a row-by-row error report on the response.

#### Scenario: Sync success

- **WHEN** an Instructor uploads 50 valid rows
- **THEN** 50 questions are created and the response shows `Success = 50, Errors = 0`

#### Scenario: Sync partial success

- **WHEN** an Instructor uploads 50 rows of which 8 are invalid
- **THEN** 42 questions are created, the response lists each invalid row with row number and reason, and no rollback occurs

#### Scenario: Reject non-xlsx

- **WHEN** an Instructor uploads a `.xls` or `.csv` or `.docx`
- **THEN** the request is rejected with a 400 and the file is not stored

### Requirement: Async import for large files

The system SHALL route any upload with more than 200 valid rows through `async-io-jobs`, persisting the file and returning a job id; the result (success count + downloadable error file) is delivered via the `import.completed` or `import.failed` notification.

#### Scenario: Submit async job

- **WHEN** an Instructor uploads a file with 1500 valid rows
- **THEN** the request returns a job id and the page shows "任务已提交，完成后将通过站内信通知"

#### Scenario: Completion notification

- **WHEN** the async job finishes
- **THEN** the Instructor receives an `import.completed` notification with the success count and a link to the error file (when applicable)

### Requirement: Append and UpdateOrAppend modes

The system SHALL support two import modes selectable at submission time: `Append` (only new questions) and `UpdateOrAppend` (rows with a known `QuestionId` update; rows without create).

#### Scenario: Append default

- **WHEN** an Instructor submits an import without specifying a mode
- **THEN** the default mode is `Append`

#### Scenario: UpdateOrAppend updates existing

- **WHEN** a row contains a `QuestionId` matching an existing question owned by the Instructor
- **THEN** the question is updated; the row id is required when mode is `UpdateOrAppend`

#### Scenario: UpdateOrAppend creates new

- **WHEN** a row does not contain a `QuestionId` and the mode is `UpdateOrAppend`
- **THEN** a new question is created

### Requirement: Ownership isolation

The system SHALL ensure imported questions are owned by the submitting Instructor; the system SHALL NOT allow an Instructor to import questions into a quiz they do not own, and SHALL NOT allow them to update questions owned by another Instructor.

#### Scenario: Non-owner denied

- **WHEN** an Instructor attempts to import into a quiz owned by another Instructor
- **THEN** the request is denied with a 403/redirect

#### Scenario: UpdateOrAppend cannot edit foreign rows

- **WHEN** a row's `QuestionId` references a question owned by another Instructor
- **THEN** the row is reported as an error with field=`QuestionId` and message=`not owner`

### Requirement: Question bank import

The system SHALL allow Admins to import questions into the central question bank (per `question-bank-admin`) using the same template, with `IsBank = true` set on imported rows.

#### Scenario: Bank import

- **WHEN** an Admin uploads a file to `/Admin/QuestionBank/Import`
- **THEN** the rows are imported as bank questions tagged with the supplied `BankTopic`

#### Scenario: Non-admin denied

- **WHEN** a non-admin user calls the bank import endpoint
- **THEN** access is denied

### Requirement: Streaming export

The system SHALL export questions to `.xlsx` using streaming writes (SXSSF) so that exports of thousands of rows do not load the full result set into memory.

#### Scenario: Filter and export

- **WHEN** an Instructor filters by question type and difficulty, then clicks Export
- **THEN** a `.xlsx` is downloaded containing matching questions

#### Scenario: Large export streamed

- **WHEN** the export would produce more than 5000 rows
- **THEN** the export runs as an async job (`async-io-jobs`) and the user is notified when the file is ready

#### Scenario: Owner-scoped export

- **WHEN** an Instructor exports their quiz's questions
- **THEN** only questions owned by that Instructor are included; another Instructor's questions are excluded

### Requirement: Error file with row numbers

The system SHALL write a downloadable `.xlsx` of invalid rows when an import completes with one or more errors; the file preserves the original row number, the offending column, and the error message.

#### Scenario: Error file produced

- **WHEN** an import finishes with `ErrorRows > 0`
- **THEN** an error file is stored and the notification / sync response includes its filename and a download link

#### Scenario: Original row number preserved

- **WHEN** a row at original position 137 fails validation
- **THEN** the error file records `RowIndex = 137` (not the post-filter index)

### Requirement: Import rate limit

The system SHALL rate-limit question imports per Instructor (default 5 imports / hour) to prevent abuse and to protect the database from bulk-write spikes.

#### Scenario: Rate limit exceeded

- **WHEN** an Instructor submits a 6th import within an hour
- **THEN** the request is rejected with a 429 and a message asking the user to retry later

### Requirement: File size and type enforcement

The system SHALL accept only `.xlsx` files up to a configured size (default 10 MB) for question imports; larger or differently-formatted files SHALL be rejected before any parsing.

#### Scenario: Oversize rejected

- **WHEN** an Instructor uploads a 12 MB `.xlsx`
- **THEN** the request is rejected with a 400 and the file is not stored

#### Scenario: Wrong type rejected

- **WHEN** an Instructor uploads a `.zip`
- **THEN** the request is rejected with a 400