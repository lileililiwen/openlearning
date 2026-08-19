## Why

Marketing campaigns need to distribute hundreds to thousands of unique coupon codes (per-channel, per-affiliate, per-event). The brief lists this as P2. The existing `commerce-extras`/`finance-admin` coupon admin page lets Admins create one coupon at a time; bulk creation via Excel saves hours.

## What Changes

- Provide an Excel import surface that creates N coupon rows in one upload. Each row may share the same `DiscountPercent` / `DiscountAmount` and `ValidFrom` / `ValidTo` but must carry a unique `Code`.
- Sync ≤200 rows; async (via `async-io-jobs`) for larger.
- Partial-success: unique-code collisions are reported per row; correct rows commit.
- Code uniqueness is enforced server-side; an existing code is reported as a row error, not overwritten.
- Append-only — there is no Update mode. Once issued, coupons are immutable (existing rule).

## Capabilities

### New Capabilities

- `coupon-bulk-import`: Excel bulk coupon creation with partial-success.

### Modified Capabilities

- `commerce-extras`: `CouponService` gains `CreateManyAsync(rows)` returning per-row results.
- `async-io-jobs` (proposed): the bulk import uses the shared async IO framework.

## Impact

- New `OpenLearning.CouponIO` module: `CouponImportJob { Id, UserId (admin), FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }`, `CouponImportRowError { Id, JobId, RowIndex, Field, Message }`. EF migration `AddCouponIO`.
- Services: `CouponImportService.ImportSyncAsync`, `CouponImportService.ImportAsync`, `CouponImportService.ProcessJobAsync`, `CouponImportTemplateService.GetTemplateBytes`.
- Pages: `Pages/Admin/Coupons/Import.cshtml(.cs)`, `Pages/Admin/Coupons/ImportJobs.cshtml(.cs)`.
- One-line DI: `builder.Services.AddCouponIOModule();`.