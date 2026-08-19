## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Invoicing` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Ecommerce`, `OpenLearning.Notifications` (never `OpenLearning.Data`)
- [x] 1.2 Add `InvoiceRequest { Id, OrderId, StudentUserId, Title, TaxId?, Type (Normal/RedLetter), Status (Requested/Rejected/Issued/Voided), CreatedAt, ReviewedAt?, ReviewedBy?, Reason?, InvoiceId? }` + `IEntityTypeConfiguration`
- [x] 1.3 Add `Invoice { Id, Number (unique), OrderId, Amount, IssuedAt, IssuedBy, VoidedAt?, VoidReason?, OriginalInvoiceId? (for red letters), Type }` + config
- [x] 1.4 Implement `InvoiceNumberService.AllocateNextAsync()` using atomic SQL `UPDATE … RETURNING` against the `system-config` table
- [x] 1.5 Implement `InvoiceRequestService.SubmitAsync`, `ReviewAsync`, `IssueAsync`, `RejectAsync`, `VoidAsync`, `IssueRedLetterAsync`
- [x] 1.6 Register `AddInvoicingModule` in `Program.cs` (one line)

## 2. Migration

- [x] 2.1 EF migration `AddInvoicing` via `dotnet ef migrations add AddInvoicing --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.2 Migrate any existing `InvoiceRequest` rows from `commerce-extras` into the new model
- [x] 2.3 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. System Config Defaults

- [x] 3.1 Add `invoice.nextNumber` (default `100000`), `invoice.prefix` (default empty), `invoice.padding` (default `6`) to the system-config defaults
- [x] 3.2 Verify the existing `Pages/Admin/System.cshtml` exposes the three parameters for editing

## 4. Pages

- [x] 4.1 `Pages/Orders/Detail.cshtml` — update the existing request flow to call `InvoiceRequestService.SubmitAsync` and show queue status
- [x] 4.2 `Pages/Admin/Invoices/Index.cshtml(.cs)` — list requests with filter by status; policy `RequireFinanceOrAdmin` (degrades to `RequireAdmin` if `ta-and-finance-roles` not merged)
- [x] 4.3 `Pages/Admin/Invoices/Review.cshtml(.cs)` — show order detail; reject with reason; issue (allocates number, creates `Invoice`, notifies student)
- [x] 4.4 `Pages/Admin/Invoices/Void.cshtml(.cs)` — void issued invoice with reason
- [x] 4.5 `Pages/Admin/Invoices/RedLetter.cshtml(.cs)` — issue red letter against an issued invoice
- [x] 4.6 `Pages/Invoices/View.cshtml(.cs)` — printable view; gated by owner / Finance / Admin
- [x] 4.7 Print stylesheet addition to `site.css` (or new `invoice.css`)

## 5. Notifications

- [x] 5.1 Send `invoice.issued` notification to student with a link to `/Invoices/{id}`
- [x] 5.2 Send `invoice.rejected` notification with reason
- [x] 5.3 Send `invoice.voided` notification with reason
- [x] 5.4 Send `invoice.red-letter-issued` notification with link to the red letter
- [x] 5.5 Add the four event types to `notification-events-extensions`

## 6. Build & Verify

- [x] 6.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 6.2 HTTP smoke tests via `curl -c/-b`:
  - Student submits a request for a paid order; request appears in admin queue
  - Finance issues; verify `Invoice` row, `InvoiceRequest.Status = Issued`, student gets notification, printable view loads
  - Finance rejects with reason; student gets notification with reason
  - Finance voids an issued invoice; verify `VoidedAt` set; printable view shows voided stamp
  - Finance issues a red letter; verify `Invoice { Type = RedLetter }` row exists; printable view shows it as negative
  - Two concurrent issue requests get distinct sequential numbers (run via xargs -P 2)
  - Non-finance user denied the admin invoice queue
  - Non-owner denied `/Invoices/{id}`
- [x] 6.3 Verify the system-config parameters render on `/Admin/System` and a change takes effect on next issue