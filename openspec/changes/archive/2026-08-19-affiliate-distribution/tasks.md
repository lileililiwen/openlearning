## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Distribution` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Ecommerce`, `OpenLearning.Notifications` (never `OpenLearning.Data`)
- [x] 1.2 Add entities: `DistributorProfile`, `AffiliateLink`, `AffiliateClick`, `Attribution`, `CommissionEntry`, `PayoutRequest`, `SettlementStatement` + `IEntityTypeConfiguration<T>` per entity
- [x] 1.3 Implement `AttributionService` (record click, lookup match by anonymous id + course + 30-day window)
- [x] 1.4 Implement `CommissionService` (create on paid order, reverse on refund, holding period transition)
- [x] 1.5 Implement `PayoutService` (request, reserve balance, admin/finance approve/reject)
- [x] 1.6 Implement `SettlementService` (freeze period, create immutable `SettlementStatement`, return per-distributor totals)
- [x] 1.7 Register `AddDistributionModule` in `OpenLearning.Web/Program.cs` (one line)
- [x] 1.8 Add `OpenLearning.Data` reference for assembly-config scan only

## 2. Public Redirect

- [x] 2.1 Create `Pages/D/C.cshtml(.cs)` (route `/D/C/{slug}`) — minimal endpoint that records the click, sets the `ol_aff` cookie, and 302-redirects to `/Courses/{courseId}`
- [x] 2.2 Return 404 for unknown slug without recording a click
- [x] 2.3 Ensure the redirect is not cached (no-store headers)

## 3. Distributor Pages

- [x] 3.1 `Pages/Distributor/Index.cshtml(.cs)` — dashboard with available balance, total earned, recent commissions; policy `RequireDistributor`
- [x] 3.2 `Pages/Distributor/Links.cshtml(.cs)` — list published courses; "Create share link" button; copy-to-clipboard helper
- [x] 3.3 `Pages/Distributor/Commissions.cshtml(.cs)` — paginated list of `CommissionEntry` rows with status filter
- [x] 3.4 `Pages/Distributor/Payouts.cshtml(.cs)` — request payout form + history list
- [x] 3.5 All four pages gated by `RequireDistributor`; the role is added via `ta-and-finance-roles`-style entry in `Roles.cs` and `DbSeeder`

## 4. Admin / Finance Pages

- [x] 4.1 `Pages/Admin/Distributors/Index.cshtml(.cs)` — list distributors; toggle `IsActive`; policy `RequireFinanceOrAdmin` (degrades to `RequireAdmin` if the TA/Finance change hasn't shipped)
- [x] 4.2 `Pages/Admin/Distributors/Payouts.cshtml(.cs)` — pending payout queue; approve / reject buttons; same policy
- [x] 4.3 `Pages/Admin/Distributors/Settlements.cshtml(.cs)` — list `SettlementStatement` rows, drill into per-distributor detail; same policy

## 5. Ecommerce Hooks

- [x] 5.1 In `OrderService.MarkPaidAsync` (or equivalent), call `CommissionService.RecordPaidAsync(orderId)` after the enrollment is created
- [x] 5.2 In `finance-admin` refund approval handler, call `CommissionService.ReverseForOrderAsync(orderId)` so reversal happens synchronously with the refund
- [x] 5.3 In `finance-admin` refund-rejection handler, no commission action

## 6. Scheduled Job Hooks

- [x] 6.1 Register an `IJob` named `distribution.commissions.hold-expire` that transitions Pending → Available when the holding period elapses (delegated to `scheduled-business-jobs` for cron registration; this change ships the `IJob` class)
- [x] 6.2 Register an `IJob` named `distribution.settlement.close-period` that closes the current period and creates `SettlementStatement` rows
- [x] 6.3 Both jobs registered via `services.AddJob<…>()` so they show up in the admin Jobs page (job-scheduler)

## 7. Notifications

- [x] 7.1 Send a `notification` when a payout request is created (to Admin/Finance)
- [x] 7.2 Send a `notification` to the Distributor when the request is approved or rejected

## 8. Migration & Build

- [x] 8.1 EF migration `AddDistribution` via `dotnet ef migrations add AddDistribution --project src/OpenLearning.Data --startup-project src/OpenLearning.Web`
- [x] 8.2 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 9. Verification

- [x] 9.1 End-to-end smoke test:
  - Promote a demo user to Distributor
  - Create a share link for a published course
  - Open `/D/C/{slug}` in a private window; verify a click row is recorded and the cookie is set
  - Sign up / log in as a different user and buy the course; verify a `CommissionEntry` is created with `Pending`
  - Wait for the holding period (or call `CommissionService.ForceTransitionAsync` in a test); commission becomes `Available`
  - Distributor requests a payout; Admin approves; verify the entry becomes `Paid` and a `SettlementStatement` is created
  - Approve a refund on the order; verify a clawback `CommissionEntry` is created with negative amount
- [x] 9.2 Verify negative scenarios:
  - Unknown slug returns 404, no click recorded
  - Order paid outside the 30-day window is unattributed
  - Distributor requesting a payout above available balance is rejected
  - Double-running the settlement job is a no-op (job-scheduler's IdempotencyKey)