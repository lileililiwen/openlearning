# Instructor Revenue — Design

## Context

Revenue data exists in orders but there is no instructor-facing settlement flow.

## Goals

- Instructors see earned revenue (total, per course, per period).
- Instructors request withdrawals of available balance.
- Admins can review and pay withdrawals.

## Non-Goals

- No actual payment processing/payouts (status changes only; money movement is external).
- No automatic settlement schedules.
- No tax/withholding handling.

## Decisions

### D1: New `OpenLearning.Settlement` module
`SettlementLedger { Id, InstructorId, CourseId?, Amount, Reason, CreatedAt }` (positive credits on paid orders, negative on refunds). `WithdrawalRequest { Id, InstructorId, Amount, Status (Pending/Paid/Rejected), RequestedAt, ReviewedAt, ReviewedBy }`. `SettlementService`: `CreditAsync(instructorId, courseId, amount, reason)`, `GetAvailableAsync(instructorId)` (sum ledger minus pending withdrawals), `RequestWithdrawalAsync`, `ListForInstructorAsync`, `ListPendingAsync`, `ReviewAsync`.

### D2: Ledger hook
The Web checkout/payment-confirm path calls `SettlementService.CreditAsync` after a paid order (composition in the Web layer, as with notifications). Refund approval (in `finance-admin`) records a negative ledger entry.

### D3: Withdrawal eligibility
Minimum available balance for withdrawal (e.g. $10) and a minimum amount; enforced in `RequestWithdrawalAsync`. Status changes notify the instructor via the notifications module.

## Risks / Trade-offs

- **Double counting** → Ledger is written only by the payment-confirm/refund paths, never recomputed from orders on the fly; documented invariant.
- **External payouts** → Withdrawal "paid" is a status flag; the actual transfer is out of scope.

## Migration Plan

One migration creates `SettlementLedger` and `WithdrawalRequests`.

## Open Questions

- Revenue share split (platform vs instructor)? MVP: 100% to instructor; a split config can be added to the ledger later.
