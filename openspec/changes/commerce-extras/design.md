# Commerce Extras — Design

## Context

The ecommerce module sells single courses. This change layers cart, refunds, coupons, balance/points, and invoices on top of the existing `Order`/`OrderService`.

## Goals

- Students build a cart and check out multiple courses at once.
- Students can request refunds and invoices on paid orders.
- Admin-defined coupons, plus balance and loyalty points, apply at checkout.
- Students see an order list with status.

## Non-Goals

- No real payment-provider refunds (refund is a request + status change; the money movement is external).
- No promo-rule engine (discounts are percent/amount per coupon).
- No printable invoice generation in MVP (request is stored; PDF later).

## Decisions

### D1: Extend the ecommerce module (no new package)
`CartItem { Id, StudentId, CourseId, AddedAt }` (unique `(StudentId, CourseId)`). `Order` gains `RefundRequestedAt`, `RefundStatus` (enum None/Requested/Approved/Rejected), `CouponId?`, `DiscountAmount`, `PaidWithBalance`, `InvoiceRequestedAt`. New `Coupon { Id, Code (unique), DiscountPercent?, DiscountAmount?, ExpiresAt?, MaxUses?, Uses }`, `CouponRedemption`, `BalanceLedger`, `PointsLedger`, `InvoiceRequest`.

### D2: Checkout flow
`/Cart` lists items; "Checkout" validates each course (published, price) then creates an order per course or one grouped order (decision: one order per course, grouped in the UI by a `CartCheckout` batch id). Coupon code validated and applied; balance and points applied in the order `Amount` minus discounts; ledgers record entries; redemptions recorded. Enrollment is created on payment confirm exactly as today.

### D3: Refunds & invoices
Student requests refund on a paid order within N days → `RefundStatus.Requested` + notification to admin. Admin review UI is part of `finance-admin`. Invoice request sets `InvoiceRequestedAt` and is stored as `InvoiceRequest` for later printing.

## Risks / Trade-offs

- **Money math** → All amounts stored as decimals; discount applied before balance; final `Amount` is what the student pays (for reporting). Rounding to 2dp at order creation.
- **Coupon abuse** → Code unique, `MaxUses` enforced atomically, single use per user per code (unique `CouponRedemption(UserId, CouponId)`).

## Migration Plan

One migration adds cart/coupon/ledger/invoice tables and the new `Order` columns.

## Open Questions

- Grouped vs per-course orders → per-course orders (reuse existing payment flow); UI groups by batch id.
