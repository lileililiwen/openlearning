# student-bulk-import Specification

## Purpose
TBD - created by archiving change student-bulk-import. Update Purpose after archive.
## Requirements
### Requirement: Excel template for student import

The system SHALL provide a downloadable Excel template with columns `Action (Create / CreateAndEnroll / EnrollExisting), Email, Phone (optional), DisplayName, Password (optional), CourseIds (semicolon-separated), ClassGroupIds (optional, semicolon-separated)`.

#### Scenario: Download template

- **WHEN** an Admin opens the student import page
- **THEN** a `.xlsx` template is returned with the headers and a sample row

#### Scenario: Action whitelist

- **WHEN** a row contains an `Action` value outside the supported set
- **THEN** the row is reported as an error

### Requirement: Sync import for small files

The system SHALL accept uploaded `.xlsx` files of up to 200 valid rows synchronously and return a row-by-row error report.

#### Scenario: Sync success

- **WHEN** an Admin uploads 50 valid `Create` rows
- **THEN** 50 accounts are created and the response shows `Success = 50, Errors = 0`

#### Scenario: Sync partial success

- **WHEN** an Admin uploads 50 rows of which 5 contain duplicate emails
- **THEN** 45 accounts are created and the 5 errors are reported with row numbers and reasons

### Requirement: Async import for large files

The system SHALL route uploads with more than 200 valid rows through `async-io-jobs`, persisting the file and returning a job id; the result is delivered via `import.completed` / `import.failed` notifications.

#### Scenario: Submit async job

- **WHEN** an Admin uploads a 1500-row file
- **THEN** the request returns a job id and the page shows "任务已提交，完成后将通过站内信通知"

#### Scenario: Completion notification

- **WHEN** the async job finishes
- **THEN** the Admin receives an `import.completed` notification with the success count and the error-file link

### Requirement: Three row-action modes

The system SHALL execute each row according to its `Action`: `Create` (account only), `CreateAndEnroll` (account + enroll), `EnrollExisting` (find user by email, skip creation).

#### Scenario: Create action

- **WHEN** an Admin uploads a `Create` row with a new email
- **THEN** an account is created and no enrollment is added

#### Scenario: CreateAndEnroll action

- **WHEN** an Admin uploads a `CreateAndEnroll` row with `CourseIds = "1;2"`
- **THEN** an account is created and the user is enrolled in courses 1 and 2

#### Scenario: EnrollExisting action

- **WHEN** an Admin uploads an `EnrollExisting` row whose email matches an existing user
- **THEN** no account is created and the user is enrolled in the listed courses

#### Scenario: EnrollExisting missing user

- **WHEN** an `EnrollExisting` row's email does not match any user
- **THEN** the row is reported as `user not found`

### Requirement: Email and phone uniqueness

The system SHALL enforce email uniqueness per row and per existing user; duplicate emails within the same upload are reported as errors. Phone numbers, when supplied, SHALL be unique among new rows.

#### Scenario: Duplicate email within file

- **WHEN** two rows in the same upload contain the same email
- **THEN** both rows are reported as `duplicate email`

#### Scenario: Email already exists for Create

- **WHEN** a `Create` row's email already exists in the database
- **THEN** the row is reported as `email already in use`; the existing account is not modified

### Requirement: Enrollment respects existing rules

The system SHALL use the existing `EnrollmentService.EnrollAsync` so paid courses require a paid order, free courses enroll directly, and `CourseGroupId` (per `class-groups`) is honoured when the row specifies it.

#### Scenario: Paid course without order

- **WHEN** a `CreateAndEnroll` row references a paid course
- **THEN** the enrollment is rejected with `course requires purchase`; the account is still created

#### Scenario: Free course

- **WHEN** a `CreateAndEnroll` row references a free course
- **THEN** the enrollment is created

#### Scenario: Class-scoped enrollment

- **WHEN** a row specifies `ClassGroupIds` matching a class group the student is being enrolled in
- **THEN** the resulting `Enrollment.ClassGroupId` is set

### Requirement: Password handling

The system SHALL allow a row to specify a `Password` value; if absent, the system SHALL generate a one-time reset link and deliver it via the welcome notification.

#### Scenario: Row specifies password

- **WHEN** a row supplies a `Password` that meets the policy
- **THEN** the account is created with that password (hashed)

#### Scenario: Password missing

- **WHEN** a row omits `Password`
- **THEN** the system generates a one-time reset token and the welcome notification includes a reset link

### Requirement: Role gating

The system SHALL allow Admin, Finance, and TA users to perform student bulk imports scoped to the courses / classes they own. Students SHALL NOT see the import page.

#### Scenario: Admin can import

- **WHEN** an Admin uploads a file
- **THEN** the request is accepted

#### Scenario: TA can import into their class

- **WHEN** a TA uploads a file at `/TA/Class/{id}/Import` for a class they are assigned to
- **THEN** only enrollments into that class are accepted; rows attempting to enroll into a different class are reported as errors

#### Scenario: Student denied

- **WHEN** a Student calls the import endpoint
- **THEN** access is denied with a 403/redirect

### Requirement: Welcome notification

The system SHALL send an `account.welcome` notification to each successfully created account with the display name, the courses enrolled, and (when applicable) a password-reset link.

#### Scenario: Welcome delivered

- **WHEN** an account is created via bulk import
- **THEN** the user receives an `account.welcome` notification

#### Scenario: Bulk-enrolled notification

- **WHEN** an existing user is enrolled via the import
- **THEN** the user receives an `enrollment.granted-bulk` notification listing the courses

### Requirement: Audit log

The system SHALL write an operation-log entry per bulk-import job recording the importer, the file key, the success count, and the error count.

#### Scenario: Audit recorded

- **WHEN** an import job finishes
- **THEN** an entry is visible in `/Admin/Logs/Operations`

