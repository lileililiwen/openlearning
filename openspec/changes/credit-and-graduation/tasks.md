# Credit and Graduation — Tasks

## 1. Domain and persistence

- [ ] 1.1 Add the Credits project, ledger/program models, configurations, and DI registration
- [ ] 1.2 Implement idempotent award, compensating revocation, and ledger query services
- [ ] 1.3 Implement versioned program rules and the degree-audit evaluator
- [ ] 1.4 Add database registration and an EF Core migration

## 2. Workflows

- [ ] 2.1 Wire configured course-completion events to idempotent credit awards
- [ ] 2.2 Add Student credit history and degree-audit pages
- [ ] 2.3 Add Admin program, adjustment, backfill-preview, and graduation-decision pages

## 3. Verification

- [ ] 3.1 Test duplicate events, revocations, rule versions, and unmet-requirement explanations
- [ ] 3.2 Build cleanly and exercise all role and negative scenarios over HTTP
