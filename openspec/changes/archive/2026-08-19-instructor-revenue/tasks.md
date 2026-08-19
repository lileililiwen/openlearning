# Instructor Revenue — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Settlement` class library, add to solution, add references (Auth, CourseManagement, Ecommerce, Notifications, EF Core)
- [x] 1.2 Add `SettlementLedger` + `WithdrawalRequest` entities + configs
- [x] 1.3 Implement `SettlementService` (credit, available balance, request, list, review)
- [x] 1.4 Register assembly scanning + `AddSettlementModule`

## 2. Revenue UI

- [x] 2.1 `/Instructor/Revenue` page: total, per-course, per-period, available balance
- [x] 2.2 Withdrawal request form + history
- [x] 2.3 Credit on payment confirm (Web composition) + notify on withdrawal review

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: paid order credits ledger, revenue page totals, withdrawal request eligibility, status transitions notify
