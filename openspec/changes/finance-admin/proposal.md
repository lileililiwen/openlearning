## Why

Admins can view a revenue report and per-course orders, but the reference system's Admin Backend requires Orders & Finance: all orders, refund review, coupon configuration, and financial reconciliation.

## What Changes

- All-orders admin page with filters (status, date, course, student) and totals.
- Refund review: process refund requests from `commerce-extras` (approve/reject, record ledger reversal for instructors).
- Coupon configuration: admin CRUD for coupons (code, discount, expiry, usage limits).
- Reconciliation: a summary comparing paid orders, refunds, and net revenue over a period (CSV export reuses `platform-analytics`).

## Capabilities

### New Capabilities
- `finance-admin`: order administration, refund review, coupon config, and reconciliation.

### Modified Capabilities

- `ecommerce`/`commerce-extras`: refund requests gain admin review; coupons gain admin CRUD.
- `platform-analytics`: reconciliation report added.

## Impact

- `OrderService` gains admin queries (all orders, refund review actions); `CouponService` gains admin CRUD; `SettlementService` gains refund reversal hook.
- Admin pages `/Admin/Orders`, `/Admin/Refunds`, `/Admin/Coupons`, `/Admin/Reconciliation`.
