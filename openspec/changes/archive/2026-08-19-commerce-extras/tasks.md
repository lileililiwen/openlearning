# Commerce Extras — Tasks

## 1. Data & Services

- [x] 1.1 Add `CartItem`, `Coupon`, `CouponRedemption`, `BalanceLedger`, `PointsLedger`, `InvoiceRequest` entities + configs
- [x] 1.2 Extend `Order` with refund/coupon/balance/invoice fields
- [x] 1.3 Implement `CartService`, `CouponService`, `LedgerService`, `InvoiceService`; extend `OrderService` (checkout-many, apply discounts, refund request)

## 2. UI

- [x] 2.1 `/Cart` page (add/remove, checkout)
- [x] 2.2 Checkout applies coupon code + balance/points with validation
- [x] 2.3 Student `/Orders` list + order details with refund/invoice actions
- [x] 2.4 Add-to-cart button on course details; cart count in nav

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: cart add/remove/checkout, coupon apply + limits, balance/points ledger, refund request status, invoice request, order list
