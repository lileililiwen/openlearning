## Why

The brief expects 发票管理 as a finance-team responsibility (admin/财务), but the existing `commerce-extras` only lets a student *request* an invoice on a paid order — there is no issuance, void, or red-letter (红冲) flow. We add the admin/finance side: review requests, issue invoices, void issued invoices, and re-issue red-letter corrections, while preserving the student's request flow.

## What Changes

- New `Invoice` entity with status (`Requested` / `Issued` / `Voided` / `RedLetter`), issue date, invoice number, tax id fields, and a reference to the original order.
- Student request flow continues to write an `InvoiceRequest { Status = Requested }`; finance reviews and either issues or rejects the request.
- Finance can void an issued invoice and, if needed, issue a red-letter correction (a negative-amount `Invoice` linked to the original).
- Invoice numbers are sequentially allocated via a system-config parameter `invoice.nextNumber` (atomic increment via SQL update).
- A printable invoice page (`/Invoices/{id}`) renders the issued invoice; the existing student request page remains at `/Orders/{id}/Invoice/Request`.

## Capabilities

### New Capabilities

- `invoice-management`: invoice entity, request → review → issue flow, void flow, red-letter correction, sequential numbering, printable invoice view.

### Modified Capabilities

- `commerce-extras`: student request now writes an `InvoiceRequest` row (status `Requested`) that finance must approve.
- `finance-admin`: the orders/refunds pages expose a link to the invoice management surface for the related order.
- `ta-and-finance-roles`: the new admin pages are gated by `RequireFinanceOrAdmin` (degrades to `RequireAdmin` until that change ships).

## Impact

- New `OpenLearning.Invoicing` module: `InvoiceRequest { Id, OrderId, StudentUserId, Title, TaxId?, Type (Normal/RedLetter), Status (Requested/Rejected/Issued/Voided), CreatedAt, ReviewedAt?, ReviewedBy?, InvoiceId? }`, `Invoice { Id, Number, OrderId, Amount, IssuedAt, IssuedBy, VoidedAt?, VoidReason? }`.
- EF migration `AddInvoicing` adds the two tables.
- Services: `InvoiceRequestService` (student submit, finance review, void), `InvoiceNumberService` (atomic next-number allocation via SQL update).
- Pages: `Pages/Orders/Detail.cshtml` exposes the request flow (existing); new `Pages/Admin/Invoices/Index.cshtml(.cs)` (queue, filter by status), `Pages/Admin/Invoices/Review.cshtml(.cs)` (issue / reject), `Pages/Invoices/View.cshtml(.cs)` (printable).
- One-line DI: `builder.Services.AddInvoicingModule();`
- No new module references `OpenLearning.Data`.