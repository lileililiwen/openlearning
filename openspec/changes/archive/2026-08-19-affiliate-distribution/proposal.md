## Why

The brief calls for a 渠道/分销 role that promotes courses via share links and earns commission. The repository has zero references to distributor / affiliate / commission (`grep -i '渠道|分销|commission|affiliate'` returns nothing in `openspec/**`), so the entire capability is missing. We add a Distributor role, a share-link attribution model, a commission ledger, and an admin review flow that pairs with the existing `job-scheduler` for periodic settlement.

## What Changes

- Add a `Distributor` role (with its own sidebar group in `navigation-chrome`).
- A distributor can generate share links per course (`/D/C/{slug}`), see clicks / signups / paid orders attributed to their links, and request payout of their available commission.
- Every paid order is attributed to a distributor (if any click in a 30-day cookie window preceded checkout).
- A commission ledger records earnings, pending payouts, and paid payouts per distributor; the periodic settlement job (`scheduled-business-jobs`) freezes a period and makes it visible to Admin/Finance for review.
- Admin (or Finance once `ta-and-finance-roles` lands) can approve/reject payout requests, which reverses the corresponding ledger entries on rejection.

## Capabilities

### New Capabilities

- `affiliate-distribution`: Distributor role, share links, click + signup attribution, commission ledger, payout request, admin/finance review.

### Modified Capabilities

- `ecommerce`: when an order is paid, `OrderService` consults the attribution store and creates a `CommissionEntry` if attribution applies.
- `navigation-chrome` (already proposed): the default sidebar gains a Distributor group under a new role.

## Impact

- New `OpenLearning.Distribution` module: `DistributorProfile { UserId, DisplayName, PayoutMethod, IsActive }`, `AffiliateLink { Id, DistributorUserId, CourseId, Slug, CreatedAt }`, `AffiliateClick { Id, LinkId, AnonymousId, IpHash, UserAgent, OccurredAt }`, `Attribution { Id, OrderId, DistributorUserId, CourseId, WindowExpiresAt }`, `CommissionEntry { Id, DistributorUserId, OrderId, CourseId, Amount, Status (Pending/Available/Paid/Reversed), CreatedAt, AvailableAt? }`, `PayoutRequest { Id, DistributorUserId, Amount, Status (Pending/Approved/Rejected/Paid), RequestedAt, ReviewedAt?, ReviewedBy? }`.
- Services: `AttributionService` (cookie-based, 30-day window), `CommissionService` (create on paid order, reserve on refund), `PayoutService` (request, review, mark paid).
- Pages: `/Distributor/Index` (dashboard), `/Distributor/Links` (per-course share URLs), `/Distributor/Commissions`, `/Distributor/Payouts`; `/Admin/Distributors/...` for review.
- New EF migration `AddDistribution` adds the tables above.
- `OrderService` gains one call to `CommissionService.RecordPaidAsync(order)`; refund approval in `finance-admin` reverses the corresponding commission (covered in `scheduled-business-jobs` for batch reversal too).
- Follows Agents.md §2.1 modular monolith pattern; no module references `OpenLearning.Data`.