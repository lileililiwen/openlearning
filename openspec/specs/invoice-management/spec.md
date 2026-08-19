# invoice-management Specification

## Purpose
TBD - created by archiving change invoice-management. Update Purpose after archive.
## Requirements
### Requirement: Invoice entity

The system SHALL persist `Invoice` rows with a unique `Number`, the originating `OrderId`, the `Amount`, the `IssuedAt` and `IssuedBy` user, and an optional `VoidedAt` and `VoidReason`.

#### Scenario: Invoice created on issue

- **WHEN** finance issues an invoice request
- **THEN** an `Invoice` row is created with a freshly allocated `Number`, `Amount`, `IssuedAt = UtcNow`, `IssuedBy = reviewerId`

#### Scenario: Number is unique

- **WHEN** two finance users issue invoices concurrently
- **THEN** both receive distinct sequential numbers (allocated atomically)

### Requirement: Student requests an invoice

The system SHALL allow a student who owns a paid order to submit an invoice request with a `Title` and an optional `TaxId`. The request is queued for finance review.

#### Scenario: Submit request

- **WHEN** a student submits a request for a paid order
- **THEN** an `InvoiceRequest { Status = Requested }` is created and the student sees a confirmation

#### Scenario: Already requested

- **WHEN** a student submits a second request for the same order
- **THEN** the existing pending request is shown and a duplicate is not created

#### Scenario: Order not paid

- **WHEN** a student submits a request for an unpaid order
- **THEN** the request is rejected

### Requirement: Finance issues an invoice

The system SHALL allow a Finance or Admin user to review a pending request and either issue an invoice (allocating the next sequential number) or reject the request with a reason.

#### Scenario: Issue request

- **WHEN** Finance issues a pending request
- **THEN** an `Invoice` row is created, the `InvoiceRequest.Status` becomes `Issued`, and the student is notified with a link to the printable invoice

#### Scenario: Reject request

- **WHEN** Finance rejects a pending request with a reason
- **THEN** the `InvoiceRequest.Status` becomes `Rejected`, the reason is stored, and the student is notified

#### Scenario: Non-finance denied

- **WHEN** a user without `Finance` or `Admin` role opens the admin invoice queue
- **THEN** access is denied

### Requirement: Issued invoices can be voided

The system SHALL allow Finance or Admin to void an issued invoice with a reason; the invoice is preserved for audit but marked `Voided`.

#### Scenario: Void an invoice

- **WHEN** Finance voids an issued invoice with a reason
- **THEN** `Invoice.VoidedAt = UtcNow`, `Invoice.VoidReason` is set, and the student is notified

#### Scenario: Already voided

- **WHEN** Finance attempts to void a voided invoice
- **THEN** the request is rejected

### Requirement: Red-letter correction

The system SHALL allow Finance to issue a red-letter (negative-amount) invoice against an existing invoice, with a reference to the original.

#### Scenario: Issue red letter

- **WHEN** Finance issues a red letter referencing the original invoice
- **THEN** a new `Invoice` row is created with `Type = RedLetter`, a fresh sequential number, and `OriginalInvoiceId` set to the referenced invoice

#### Scenario: Printable view shows red letter

- **WHEN** a student opens the red-letter invoice
- **THEN** the printable view shows it as a negative-amount correction with the original invoice number referenced

### Requirement: Printable invoice view

The system SHALL provide a printable view at `/Invoices/{id}` showing the invoice number, issue date, buyer, items, total, and (for red letters) the original invoice reference.

#### Scenario: Open printable view

- **WHEN** an authorised user (invoice owner, Finance, Admin) opens `/Invoices/{id}`
- **THEN** the printable view is shown

#### Scenario: Non-owner denied

- **WHEN** a user other than the owner, Finance, or Admin opens `/Invoices/{id}`
- **THEN** access is denied

### Requirement: Sequential numbering is configurable

The system SHALL read the next invoice number from a system-config parameter `invoice.nextNumber`; issuing an invoice atomically increments it.

#### Scenario: Auto-increment on issue

- **WHEN** Finance issues an invoice
- **THEN** the system increments `invoice.nextNumber` by 1 within the same transaction as the invoice insert

#### Scenario: Default value

- **WHEN** `invoice.nextNumber` is unset
- **THEN** it defaults to `100000` (matching common practice)

### Requirement: Invoice numbering prefix

The system SHALL allow the invoice number format to include a configurable prefix (`invoice.prefix`, default empty) and a configurable padding width (`invoice.padding`, default 6).

#### Scenario: Format with prefix

- **WHEN** the prefix is `OL` and padding is 8
- **THEN** the first issued invoice is `OL00010000`

#### Scenario: No prefix

- **WHEN** the prefix is empty and padding is 6
- **THEN** the first issued invoice is `100000`

