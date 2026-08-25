# Peer Assessment — Tasks

## 1. Domain

- [x] 1.1 Add the PeerAssessment project with configuration, rubric question, allocation run, allocation pair, assessment, and result models plus configurations
- [x] 1.2 Implement phase state transitions, deterministic self-free reviewer allocation with shortfall reporting, and re-run handling
- [x] 1.3 Implement assessment submission guards (allocation, enrollment, phase) and server-side final-score combination with override
- [x] 1.4 Add database registration and an EF Core migration

## 2. Workflows

- [x] 2.1 Add Instructor pages: enable/configure peer review, run allocation, view progress and shortfalls, release results, apply overrides
- [x] 2.2 Add Student pages: review queue of allocated submissions, rubric assessment form, received-assessment and final-result views honoring anonymity and release
- [x] 2.3 Wire notifications for review-phase open and results released using existing notification events

## 3. Verification

- [x] 3.1 Test allocation completeness/self-freeness, phase gating, unallocated denial, anonymity, early-release denial, override precedence, and non-owner denial
- [x] 3.2 Build cleanly and exercise every scenario over HTTP
