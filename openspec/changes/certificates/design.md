# Certificates — Design

## Context

Progress is fully tracked (lessons + quizzes + SCORM feed the same completion percentage), but there is no completion credential. This change issues a printable certificate when a course reaches 100%.

## Goals

- Automatic certificate issuance at 100% progress.
- Printable HTML certificate with the student's name, course title, and a unique code.
- Certificate history visible to the student.

## Non-Goals

- No PDF generation for the MVP (print-to-PDF from the browser is acceptable).
- No verification URLs or public credential registry (deferred; the `Code` field supports it later).
- No instructor/admin-issued certificates.

## Decisions

### D1: New `OpenLearning.Certificates` module
`Certificate { Id, EnrollmentId (unique), CourseId, UserId, IssuedAt, Code }`. `Code` is a short random token (e.g., `CRT-XXXXXX`) generated at issuance for future verification. Unique per enrollment prevents duplicates.

### D2: Issuance
`CertificateService.EnsureIssuedAsync(enrollmentId)` checks progress (via `ProgressService.GetProgressPercentAsync` == 100) and inserts a certificate if absent. Called from the course details page and student dashboard — idempotent. Rationale: no background job needed at MVP scale.

### D3: Certificate page
A standalone printable HTML page (`/Certificates/View?id=`) with the student's display name, course title, instructor name, completion date, and code. Access: the student who earned it, the course owner, and admins. CSS `@media print` trims chrome.

## Risks / Trade-offs

- **Idempotency** → Unique enrollment index + `EnsureIssued` (check-then-insert) is atomic enough under MVP concurrency.
- **Fake certificates** → The `Code` field is a placeholder for future public verification; not actively validated in MVP.

## Migration Plan

One migration creates `Certificates`.

## Open Questions

- Should certificates reflect quiz passing thresholds? Not in MVP — completion is 100% progress.
