## Context

The current `commerce-extras` spec stops at "the request is stored for later review". The brief extends this to a real invoice workflow: review, issue, void, red-letter. We model this with two entities (`InvoiceRequest` for the queue, `Invoice` for issued records) so the request lifecycle and the issued-invoice lifecycle are separate concerns.

Sequential numbering is a familiar pattern; we use a SQL `UPDATE … SET next = next + 1 RETURNING next` so concurrent issuance cannot produce duplicates. The printable view reuses the existing `site.css` print stylesheet.

## Goals / Non-Goals

**Goals:**
- Full invoice lifecycle: request → review → issue → void / red-letter.
- Atomic sequential numbering.
- Printable view.
- Hook into the existing `finance-admin` surface and the `ta-and-finance-roles` policy.

**Non-Goals:**
- Integration with external e-invoicing providers (e.g. 发票真伪查验). The Invoice record is internal.
- Multi-currency invoicing.
- Bulk import of legacy invoices.

## Decisions

- **Two tables, not one.** `InvoiceRequest` (queue + lifecycle) and `Invoice` (issued record) keeps the "is this a request or an issued invoice?" distinction clear. An issued invoice is referenced by `InvoiceRequest.InvoiceId`.
- **Atomic number allocation via SQL update.** Single Postgres instance makes `UPDATE system_config SET value = (value::int + 1)::text WHERE key='invoice.nextNumber' RETURNING value` correct and lock-free.
- **Prefix + padding** configurable so admins can match local formats.
- **Red letter is its own row** rather than a flag — easier to audit; the original is preserved and references the red letter.
- **Permission policy**: `RequireFinanceOrAdmin` (degrades to `RequireAdmin` if `ta-and-finance-roles` is not merged).

## Risks / Trade-offs

- [Risk: a finance user issues an invoice against the wrong order] → Mitigation: the review page shows the order detail (buyer, amount, line items) before the issue button.
- [Risk: voiding cascades to refund-related ledger entries] → Mitigation: voiding does NOT auto-refund; finance issues a refund separately if needed (existing `finance-admin` flow).
- [Risk: invoice numbering reset on rollback] → Mitigation: the increment + insert share a single transaction; either both commit or neither does.

## Migration Plan

1. Add `OpenLearning.Invoicing` module + EF migration `AddInvoicing`.
2. Migrate existing `InvoiceRequest` rows from `commerce-extras` into the new model (status mapping: any existing requests become `Requested`).
3. Wire the finance pages.
4. Verify the printable view renders correctly.

## Open Questions

- Should issued invoices be downloadable as PDF? Out of scope; browser print-to-PDF covers most needs. A future `invoice-pdf-export` change can add it.
- Should the red letter reverse the related refund commission? No — that's the refund flow's job; the invoice just records the correction.