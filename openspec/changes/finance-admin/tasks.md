# Finance Admin — Tasks

## 1. Services

- [ ] 1.1 `OrderService.GetAllAsync` (filters, pagination, totals) + `ReviewRefundAsync` (approve/reject)
- [ ] 1.2 Add `OrderStatus.Refunded`; refund approval posts negative settlement entry (Web composition)
- [ ] 1.3 Coupon admin CRUD (via `CouponService`)

## 2. Admin UI

- [ ] 2.1 `/Admin/Orders`: all orders with filters + totals
- [ ] 2.2 `/Admin/Refunds`: requested refunds, approve/reject, notify student
- [ ] 2.3 `/Admin/Coupons`: create/edit/deactivate coupons
- [ ] 2.4 `/Admin/Reconciliation`: gross/refunds/net per course + totals + CSV

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration (enum change only)
- [ ] 3.2 Build, start app, verify: order filters, refund approve reverses instructor ledger and notifies, coupon CRUD, reconciliation totals, non-admin denied
