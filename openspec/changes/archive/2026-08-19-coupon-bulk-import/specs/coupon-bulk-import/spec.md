## ADDED Requirements

### Requirement: Excel template for coupons

The system SHALL provide a downloadable Excel template with columns `Code, DiscountType (Percent/Amount), DiscountValue, ValidFrom, ValidTo, MaxRedemptions?`.

#### Scenario: Download template

- **WHEN** an Admin opens the bulk import page
- **THEN** a `.xlsx` template is returned

### Requirement: Sync import for small files

The system SHALL accept uploads of up to 200 valid rows synchronously and return row-by-row errors.

#### Scenario: Sync success

- **WHEN** an Admin uploads 100 valid rows
- **THEN** 100 coupons are created

#### Scenario: Sync partial success

- **WHEN** 100 rows are uploaded of which 6 collide with existing codes
- **THEN** 94 coupons are created and the 6 errors are reported with row numbers

### Requirement: Async import for large files

The system SHALL route uploads with more than 200 valid rows through `async-io-jobs` (per `async-io-jobs`); the result is delivered via `import.completed` / `import.failed` notifications.

#### Scenario: Submit async job

- **WHEN** an Admin uploads a 1500-row file
- **THEN** the request returns a job id and the page shows "任务已提交，完成后将通过站内信通知"

### Requirement: Code uniqueness

The system SHALL enforce `Code` uniqueness across the platform; a row with a `Code` already in use SHALL be reported as an error.

#### Scenario: Duplicate within file

- **WHEN** two rows in the same upload share the same `Code`
- **THEN** both rows are reported as `duplicate code`

#### Scenario: Existing code

- **WHEN** a row's `Code` exists in the database
- **THEN** the row is reported as `code already exists`; the existing coupon is not modified

### Requirement: Append-only

The system SHALL provide only an append mode; coupon values cannot be updated via this import path.

#### Scenario: No update

- **WHEN** an Admin uploads a row whose `Code` matches an existing coupon
- **THEN** the row is reported as an error (not updated)

### Requirement: Validation rules

The system SHALL validate: `Code` 4–32 chars and `[A-Za-z0-9_-]` only; `DiscountType ∈ {Percent, Amount}`; `DiscountValue > 0`; `ValidFrom < ValidTo`; `MaxRedemptions ≥ 1` when supplied.

#### Scenario: Invalid code format

- **WHEN** a row's `Code` contains spaces or special characters
- **THEN** the row is reported as `invalid code format`

#### Scenario: Invalid date range

- **WHEN** `ValidFrom >= ValidTo`
- **THEN** the row is reported as `invalid date range`

### Requirement: Ownership and rate limit

The system SHALL allow only Admins to perform the bulk import; rate-limited to 5 imports / hour per Admin.

#### Scenario: Non-admin denied

- **WHEN** a non-admin user calls the endpoint
- **THEN** access is denied

#### Scenario: Rate limit exceeded

- **WHEN** an Admin submits a 6th import within an hour
- **THEN** the request is rejected with a 429

### Requirement: File safety

The system SHALL accept only `.xlsx` files up to 5 MB.

#### Scenario: Oversize rejected

- **WHEN** an Admin uploads a 7 MB file
- **THEN** the request is rejected

### Requirement: Audit log

The system SHALL write an `OperationLog` entry per import job (importer, file key, success / error counts).

#### Scenario: Audit recorded

- **WHEN** an import job finishes
- **THEN** an entry is visible in `/Admin/Logs/Operations`