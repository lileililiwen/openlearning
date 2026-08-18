# certificates Specification

## Purpose
TBD - created by archiving change certificates. Update Purpose after archive.
## Requirements
### Requirement: Certificate is issued on completion

The system SHALL issue a certificate to a Student when their progress in a course reaches 100%, once per enrollment.

#### Scenario: Complete course
- **WHEN** a Student's progress in a course reaches 100%
- **THEN** a certificate is issued with the course title, student name, completion date, and a unique code

#### Scenario: No duplicate certificates
- **WHEN** a completed course is viewed again
- **THEN** no additional certificate is issued

### Requirement: Student can view and print certificates

The system SHALL allow a Student to open and print a certificate they earned.

#### Scenario: Open certificate
- **WHEN** a Student opens their certificate for a completed course
- **THEN** a printable certificate is shown

#### Scenario: Certificate history
- **WHEN** a Student views their dashboard or profile
- **THEN** their earned certificates are listed

