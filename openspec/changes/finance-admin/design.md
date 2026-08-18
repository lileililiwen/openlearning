# Finance Admin — Design

## Context

Orders exist but admins can't manage them centrally; refunds and coupons are requested/defined but not reviewed/maintained; reconciliation is manual.

## Goals

- Admins see every order and filter by status/date/course/student.
- Admins approve or reject refund requests.
- Admins manage coupons.
- Reconciliation summarizes orders/refunds/net per period.

## Non-Goals

- No payment-provider integration (order statuses remain manual flags).
- No multi-currency handling.
- No automated settlement (manual review).

## Decisions

### D1: Extend existing services (no new package)
`OrderService.GetAllAsync(filters, page)` (admin), `ReviewRefundAsync(orderId, approve, adminId)`. On approve: set `RefundStatus=Approved`, `Status=Refunded` (new `OrderStatus.Refunded`), call `SettlementService` to post a negative ledger entry for the instructor. `CouponService` admin CRUD already defined in `commerce-extras`; this change adds the admin pages that call it.

### D2: Refund review flow
`commerce-extras` sets `RefundRequestedAt` + `RefundStatus.Requested`; `/Admin/Refunds` lists requested refunds; approve/reject sets status and notifies the student.

### D3: Reconciliation report
`/Admin/Reconciliation?from&to` computes: paid order count + gross, refunded count + amount, net = gross − refunds, by course and totals. CSV export reuses the `platform-analytics` `CsvHelper`.

## Risks / Trade-offs

- **Ledger consistency** → Refund approval posts a negative settlement entry and marks the order Refunded atomically (single save).
- **Manual statuses** → Documented that payment movement is external; the UI reflects platform state only.

## Migration Plan

One migration adds `OrderStatus.Refunded` (enum value) — no new tables.

## Open Questions

- Refund eligibility window (e.g. 30 days)? Enforced in `commerce-extras` request flow; admin can override.
