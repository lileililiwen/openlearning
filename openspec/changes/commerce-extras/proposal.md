## Why

Purchases are single-course "Buy Now" only. The reference system's Order & Payment module requires a shopping cart, order list/details, refund requests, coupons, loyalty points, account balance, discounts, and invoices. These are standard e-commerce features that increase conversion and support promotions.

## What Changes

- Shopping cart: add/remove courses, then checkout one or more items (creates one order per course, or a grouped order).
- Order list/details page for students; existing order records are surfaced with richer status.
- Refund requests: student requests a refund on a paid order; admin reviews it (`finance-admin` change provides the admin review surface).
- Coupons: admin-defined discount codes applied at checkout.
- Account balance + loyalty points: balance can pay for orders; points accrue per purchase and can be used as a discount.
- Invoice requests: students can request an invoice for a paid order (stored, printable later).

## Capabilities

### New Capabilities
- `commerce-extras`: shopping cart, refund requests, coupons, balance/points, invoices.

### Modified Capabilities

- `ecommerce`: checkout accepts cart items and coupon/balance/points; order gains status fields; student order list.

## Impact

- `Order` gains `RefundRequestedAt`, `RefundStatus`, `InvoiceRequestedAt`; new `CartItem { Id, StudentId, CourseId, AddedAt }`, `Coupon { Id, Code, DiscountPercent/Amount, ExpiresAt, MaxUses }`, `CouponRedemption`, `BalanceLedger { Id, UserId, Amount, Reason, CreatedAt }`, `PointsLedger`, `InvoiceRequest`.
- `OrderService` extends: cart add/remove, checkout-many, apply coupon/balance/points, refund request; `CouponService`, `LedgerService`, `InvoiceService`.
- Pages: `/Cart`, `/Orders` list, `/Orders/{id}` details, refund/invoice actions; checkout applies discounts.
