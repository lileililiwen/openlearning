# Finance Admin — Tasks

## 1. Services

- [x] 1.1 `OrderService.GetAllAsync` (filters, pagination, totals) + `ReviewRefundAsync` (approve/reject)
- [x] 1.2 Add `OrderStatus.Refunded`; refund approval posts negative settlement entry (Web composition)
- [x] 1.3 Coupon admin CRUD (via `CouponService`)

## 2. Admin UI

- [x] 2.1 `/Admin/Orders`: all orders with filters + totals
- [x] 2.2 `/Admin/Refunds`: requested refunds, approve/reject, notify student
- [x] 2.3 `/Admin/Coupons`: create/edit/deactivate coupons
- [x] 2.4 `/Admin/Reconciliation`: gross/refunds/net per course + totals + CSV

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration (enum change only)
- [x] 3.2 Build, start app, verify: order filters, refund approve reverses instructor ledger and notifies, coupon CRUD, reconciliation totals, non-admin denied
