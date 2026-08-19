## ADDED Requirements

### Requirement: Assignment submissions export

The system SHALL export an Instructor's assignment submissions to `.xlsx` with columns `StudentEmail, StudentName, AssignmentTitle, SubmittedAt, Status, Score, Feedback, IsLate`.

#### Scenario: Owner exports

- **WHEN** the owning Instructor opens `/Courses/{id}/Assignments/{aid}/Export`
- **THEN** a `.xlsx` is downloaded containing every submission to that assignment

#### Scenario: Date filter

- **WHEN** the Instructor filters by date range
- **THEN** only submissions within the range are exported

#### Scenario: Non-owner denied

- **WHEN** another Instructor opens the export endpoint
- **THEN** access is denied

### Requirement: Quiz attempts export

The system SHALL export quiz attempts to `.xlsx` with columns `StudentEmail, StudentName, QuizTitle, AttemptedAt, ScorePercent, Passed, PerQuestionJson`.

#### Scenario: Per-quiz export

- **WHEN** the owning Instructor opens `/Courses/{id}/Quizzes/{qid}/Export`
- **THEN** a `.xlsx` is downloaded containing every attempt for that quiz

#### Scenario: Per-course export

- **WHEN** the Instructor opens `/Courses/{id}/Quizzes/Export` (course-wide)
- **THEN** one row per attempt across all quizzes in the course is produced

### Requirement: Exam attempts export

The system SHALL export exam attempts (per pending `exams`) to `.xlsx` with columns `StudentEmail, StudentName, ExamTitle, StartedAt, SubmittedAt, ScorePercent, Passed, ScreenSwitchCount, PerQuestionJson`.

#### Scenario: Per-exam export

- **WHEN** the owning Instructor opens the exam export page
- **THEN** a `.xlsx` is downloaded containing every attempt for that exam

### Requirement: Course-grade roster export

The system SHALL export a per-course roster with the learner's final grade (per `certificates` / `progress-tracking`) and last activity.

#### Scenario: Roster export

- **WHEN** the Instructor opens `/Courses/{id}/Roster/Export`
- **THEN** a `.xlsx` is downloaded with one row per enrollment: `StudentEmail, StudentName, EnrolledAt, LastActivityAt, ProgressPercent, FinalScore, CertificateNumber?`

#### Scenario: Class-scoped roster export

- **WHEN** a TA opens `/TA/Class/{id}/Export` for a class they are assigned to
- **THEN** only enrollments for that class are included

### Requirement: Sync ceiling and async fallback

The system SHALL stream the export for ≤1000 rows synchronously and route exports with more than 1000 rows through `async-io-jobs`, delivering the file via the `export.ready` notification.

#### Scenario: Sync download

- **WHEN** the filter set would produce 800 rows
- **THEN** the file is streamed in the response and the browser downloads it immediately

#### Scenario: Async fallback

- **WHEN** the filter set would produce 5000 rows
- **THEN** the request returns a job id and the page shows "文件正在生成，完成后将通过站内信通知"

#### Scenario: Notification on completion

- **WHEN** the async export finishes
- **THEN** the requester receives an `export.ready` notification with a download link valid for 7 days

### Requirement: Streaming writes

The system SHALL use SXSSF streaming for all grade exports so memory usage does not scale with row count.

#### Scenario: Memory bounded

- **WHEN** an export produces 50,000 rows
- **THEN** memory usage remains under 100 MB on the server (verified via a smoke-test marker in the task)

### Requirement: Ownership and row isolation

The system SHALL enforce row-level ownership: an Instructor only sees submissions/attempts for courses they own; a TA only sees results for classes they are assigned to.

#### Scenario: TA cannot export foreign class

- **WHEN** a TA opens `/TA/Class/{id}/Export` for a class they are not assigned to
- **THEN** access is denied

#### Scenario: Instructor cannot export foreign course

- **WHEN** an Instructor who does not own the course attempts to export
- **THEN** access is denied

### Requirement: No import counterpart

The system SHALL NOT provide an import surface for submissions, attempts, or rosters. Answer data is produced only by student activity in the platform.

#### Scenario: Import endpoint absent

- **WHEN** an Instructor searches for an "import grades" link
- **THEN** no such link exists; only export is available

### Requirement: File retention

The system SHALL retain exported files for 7 days via `scheduled-business-jobs` cleanup; the download link expires after that period.

#### Scenario: Expired link

- **WHEN** a user clicks a download link after 7 days
- **THEN** the page returns 404

### Requirement: Audit log

The system SHALL write an operation-log entry per export job (who exported, what filters, the row count).

#### Scenario: Audit recorded

- **WHEN** an export job finishes
- **THEN** an entry is visible in `/Admin/Logs/Operations`