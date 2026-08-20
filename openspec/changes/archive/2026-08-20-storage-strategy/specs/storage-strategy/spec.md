## ADDED Requirements

### Requirement: Configurable storage strategy

The system SHALL let an Admin choose the active storage backend (Local,
S3-compatible, or Aliyun OSS) and persist that choice; the change SHALL apply
on the next application start.

#### Scenario: Select a strategy

- **WHEN** an Admin opens `/Admin/Storage` and selects `Local`, `S3`, or
  `Aliyun OSS` and saves
- **THEN** the choice is persisted and the page shows that the change takes
  effect after a restart

#### Scenario: Restart applies the strategy

- **WHEN** the application is restarted after an Admin selected `S3`
- **THEN** uploads and the `/files` proxy use the S3 backend

#### Scenario: Default is local

- **WHEN** no strategy has ever been configured
- **THEN** uploads use the local disk backend

### Requirement: S3-compatible provider

The system SHALL support any S3-compatible endpoint (including AWS S3 and
MinIO) with configurable endpoint, bucket, access key, secret, region, and
path-style addressing.

#### Scenario: Upload to S3

- **WHEN** the S3 backend is active and an upload happens
- **THEN** the object is stored at key `{purpose}/{guid}{ext}` in the
  configured bucket

#### Scenario: Serve from S3

- **WHEN** a user requests `/files/{key}` for an S3-stored file
- **THEN** the proxy streams the object from S3

#### Scenario: Delete from S3

- **WHEN** a file is deleted
- **THEN** the object (and any `low/mid/high` rendition objects) are removed
  from the bucket

### Requirement: MinIO provider (self-hosted built-in OSS)

The system SHALL support MinIO as an explicit, first-class storage strategy;
MinIO is an S3-compatible endpoint with path-style addressing forced on, and a
default endpoint of `http://localhost:9000` is offered in the admin page.

#### Scenario: Select MinIO

- **WHEN** an Admin selects `MinIO` on the storage page
- **THEN** the endpoint defaults to `http://localhost:9000`, path-style
  addressing is forced on, and the S3/MinIO option fields are shown

#### Scenario: Upload/serve/delete via MinIO

- **WHEN** the MinIO backend is active
- **THEN** uploads land in the MinIO bucket, `/files/{key}` streams them, and
  delete removes the object and rendition objects

### Requirement: Aliyun OSS provider

The system SHALL support Aliyun OSS with configurable endpoint, bucket, access
key, and secret.

#### Scenario: Upload to OSS

- **WHEN** the Aliyun OSS backend is active and an upload happens
- **THEN** the object is stored at key `{purpose}/{guid}{ext}` in the
  configured bucket

#### Scenario: Serve and delete from OSS

- **WHEN** a file is requested or deleted
- **THEN** the proxy streams from OSS, and delete removes the object and
  rendition objects

### Requirement: Connectivity test

The system SHALL let an Admin test the configured backend by writing, reading,
and deleting a probe object, surfacing a clear success or failure.

#### Scenario: Test succeeds

- **WHEN** the Admin clicks "测试连接" with valid credentials
- **THEN** a success message is shown and the probe object is removed

#### Scenario: Test fails

- **WHEN** the credentials or endpoint are invalid
- **THEN** a failure message is shown and the secrets are not echoed in the
  error

### Requirement: Configurable upload limits

The system SHALL make per-purpose maximum size and allowed extensions
configurable via system-config, falling back to the current defaults.

#### Scenario: Raise the image limit

- **WHEN** an Admin raises `Storage.Limits.Image.MaxBytes`
- **THEN** image uploads up to the new limit are accepted

#### Scenario: Restrict an extension

- **WHEN** an Admin removes `.svg` from the image extension list
- **THEN** `.svg` image uploads are rejected

### Requirement: Backend transparency

The system SHALL keep existing URLs, upload callers, and video renditions
working unchanged for any active backend; rendition objects are written through
the active provider.

#### Scenario: Existing URLs unchanged

- **WHEN** the backend switches from Local to S3
- **THEN** `/files/{key}` and rendition URLs still serve correctly through the
  proxy

### Requirement: Secret handling

The system SHALL store provider credentials server-side only, mask them in the
admin UI after saving, and never log them.

#### Scenario: Masked after save

- **WHEN** an Admin saves an access key and reopens `/Admin/Storage`
- **THEN** the secret field shows a masked placeholder, not the value

#### Scenario: Clear a secret

- **WHEN** an Admin uses "清除密钥"
- **THEN** the stored secret is removed and the field is empty
