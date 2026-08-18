## Why

Progress tracking already computes completion, but students have nothing to show for it. Certificates are the standard credential an online course provides and a key motivator for finishing.

## What Changes

- When a Student reaches 100% progress in a course, a certificate is issued automatically.
- Students can view and print/download a certificate (HTML; PDF export is a future option) from the course or their dashboard/profile.
- Certificates are listed on the student dashboard and profile.

## Capabilities

### New Capabilities
- `certificates`: automatic issuance on course completion, certificate viewing, and certificate history for students.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Certificates` module: `Certificate { Id, EnrollmentId, CourseId, UserId, IssuedAt, Code }` (unique per enrollment); `CertificateService` (issue-if-complete, get for enrollment/user).
- Certificate page (`/Certificates/View`) rendering a printable HTML certificate; links from course details, dashboard, and profile.
- No changes to progress calculation.
