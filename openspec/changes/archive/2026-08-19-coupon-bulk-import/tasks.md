## 1. Dependencies

- [x] 1.1 Confirm `async-io-jobs` is merged

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.CouponIO` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Ecommerce`, `OpenLearning.Jobs` (never `OpenLearning.Data`)
- [x] 2.2 Add `CouponImportJob { Id, UserId (admin), FileKey, Status, TotalRows, SuccessRows, ErrorRows, ErrorFileKey?, CreatedAt, FinishedAt? }` + config
- [x] 2.3 Add `CouponImportRowError { Id, JobId, RowIndex, Field, Message }` + config
- [x] 2.4 EF migration `AddCouponIO` via `dotnet ef migrations add AddCouponIO --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 2.5 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 3. Service Layer

- [x] 3.1 Implement `CouponImportService.ImportSyncAsync(file, adminId)` returning `(successCount, errors[])`
- [x] 3.2 Implement `CouponImportService.ImportAsync(file, adminId)` — wraps `AsyncIOService.SubmitAsync(Kind = "CouponImport", ...)`
- [x] 3.3 Implement `CouponImportService.ProcessJobAsync(jobId)` — parses, validates, persists, writes the error file
- [x] 3.4 Implement `CouponImportTemplateService.GetTemplateBytes()`
- [x] 3.5 Implement `CouponImportRateLimiter` reading `coupon.import.rateLimitPerHour` from `system-config` (default 5)

## 4. Validation Rules

- [x] 4.1 `Code` matches `^[A-Za-z0-9_-]{4,32}$`
- [x] 4.2 `DiscountType ∈ {Percent, Amount}`
- [x] 4.3 `DiscountValue > 0`
- [x] 4.4 `ValidFrom < ValidTo`
- [x] 4.5 `MaxRedemptions ≥ 1` when supplied

## 5. Pages

- [x] 5.1 `Pages/Admin/Coupons/Import.cshtml(.cs)` — file upload, sync vs async, inline error preview
- [x] 5.2 `Pages/Admin/Coupons/ImportJobs.cshtml(.cs)` — recent jobs
- [x] 5.3 `Pages/Admin/Coupons/Template.cshtml(.cs)` — streams the template

## 6. File Safety

- [x] 6.1 Accept only `.xlsx`, max 5 MB (config: `coupon.import.maxBytes`)

## 7. Audit

- [x] 7.1 Write `OperationLog` row per finished import job

## 8. Build & Verify

- [x] 8.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 8.2 HTTP smoke tests:
  - Admin uploads 100 valid rows → 100 coupons created
  - Admin uploads 100 rows with 6 colliding codes → 94 created, error file lists 6
  - Admin uploads 1500 rows → async job id; `import.completed` notification delivered
  - Non-admin denied
  - Admin submits 6 imports / hour → 6th returns 429
  - `.csv` rejected
  - 7 MB rejected
  - Audit log entry visible