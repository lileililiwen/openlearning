# course-access-period Specification

## Purpose
TBD - created by archiving change course-access-period. Update Purpose after archive.
## Requirements
### Requirement: Enrollment carries an access expiry

The system SHALL store an `AccessExpiresAt` (nullable) on each `Enrollment` row. When `AccessExpiresAt` is set and is earlier than `UtcNow + graceDays`, the learner SHALL be treated as expired.

#### Scenario: Default unlimited

- **WHEN** a course has `DefaultAccessDays = null`
- **THEN** new enrollments for that course have `AccessExpiresAt = null` (no expiry)

#### Scenario: Course default seeds expiry

- **WHEN** a course has `DefaultAccessDays = 180` and a student enrolls
- **THEN** the new enrollment has `AccessExpiresAt = UtcNow + 180 days`

#### Scenario: Override per enrollment

- **WHEN** an Instructor or Admin sets `AccessExpiresAt` directly on an enrollment
- **THEN** the override takes precedence over the course default

#### Scenario: Membership-based expiry

- **WHEN** a Student with an active membership enrolls in a paid course using the membership benefit
- **THEN** the enrollment's `AccessExpiresAt = min(Membership.ExpiresAt, course default)`

### Requirement: Grace period allows read-only access

The system SHALL allow a learner whose enrollment is past `AccessExpiresAt` but within `enrollment.expiry.graceDays` (default 3) to view course content read-only and SHALL NOT allow new attempts, new submissions, or marking lessons complete.

#### Scenario: Grace period banner

- **WHEN** a learner opens `/MyCourses` while inside the grace period
- **THEN** the page shows a banner "您的课程 X 将在 N 天后收回访问权限，请及时续费"

#### Scenario: Block write actions during grace

- **WHEN** a learner in the grace period attempts to mark a lesson complete or start a quiz attempt
- **THEN** the request is denied with a message directing them to renew

### Requirement: Expired enrollments are revoked by the expiry job

The system SHALL revoke an enrollment when `UtcNow > AccessExpiresAt + graceDays` via the `enrollment.expiry.revoke` job registered with `job-scheduler`, and SHALL set `RevokedAt` and `RevokedReason = "expired"`.

#### Scenario: Revoke expired enrollment

- **WHEN** the expiry job runs and finds an enrollment past `AccessExpiresAt + graceDays`
- **THEN** the enrollment is marked `Revoked` and the learner is notified

#### Scenario: Skip already revoked

- **WHEN** the expiry job processes an already-revoked enrollment
- **THEN** it is skipped (idempotent)

#### Scenario: Read history preserved

- **WHEN** an enrollment is revoked
- **THEN** the learner's prior progress, attempts, scores, and certificates remain viewable

### Requirement: Re-enrollment after expiry

The system SHALL allow a learner whose enrollment was revoked for expiry to re-enroll by purchasing the course again (or, for free courses, by direct re-enrollment).

#### Scenario: Re-enroll a revoked student

- **WHEN** a learner whose enrollment is `Revoked` attempts to enroll in the same course
- **THEN** the request is accepted and a new enrollment row is created

#### Scenario: Active enrollment still blocks

- **WHEN** a learner with an active (non-revoked) enrollment attempts to enroll again
- **THEN** the request is rejected per the existing duplicate-enrollment rule

#### Scenario: Membership benefit re-applies

- **WHEN** a learner with an active membership re-enrolls in a paid course after expiry
- **THEN** the membership benefit re-applies and `AccessExpiresAt = min(Membership.ExpiresAt, course default)`

### Requirement: Admin can revoke an enrollment manually

The system SHALL allow an Admin or Finance user to revoke an active enrollment with a reason (`RevokedReason`); the learner is notified and the record keeps its history.

#### Scenario: Manual revoke

- **WHEN** an Admin revokes an enrollment with reason "refund"
- **THEN** the enrollment becomes `Revoked`, the learner is notified, and history is preserved

#### Scenario: Revoke is irreversible without admin action

- **WHEN** a learner whose enrollment was manually revoked attempts to re-enroll
- **THEN** they can re-enroll (same rules as expiry revocation)

### Requirement: Admin configures the grace period

The system SHALL read `enrollment.expiry.graceDays` from the existing `system-config` value store; the default is 3 days; Admins can change it from the existing system-config UI.

#### Scenario: Update grace days

- **WHEN** an Admin sets `enrollment.expiry.graceDays = 7`
- **THEN** new expiry evaluations use the 7-day grace; existing in-flight enrollments re-evaluate on the next job tick

