# Instructor Revenue — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Settlement` class library, add to solution, add references (Auth, CourseManagement, Ecommerce, Notifications, EF Core)
- [ ] 1.2 Add `SettlementLedger` + `WithdrawalRequest` entities + configs
- [ ] 1.3 Implement `SettlementService` (credit, available balance, request, list, review)
- [ ] 1.4 Register assembly scanning + `AddSettlementModule`

## 2. Revenue UI

- [ ] 2.1 `/Instructor/Revenue` page: total, per-course, per-period, available balance
- [ ] 2.2 Withdrawal request form + history
- [ ] 2.3 Credit on payment confirm (Web composition) + notify on withdrawal review

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Build, start app, verify: paid order credits ledger, revenue page totals, withdrawal request eligibility, status transitions notify
