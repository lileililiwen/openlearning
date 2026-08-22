# Practical Training — Design

## Context

Practical learning spans a learner, academic coordinator, and external supervisor. External supervisors need narrow access without becoming broad instructors.

## Goals

- Manage placement lifecycle and competency plans.
- Collect auditable hours and evidence with multi-party approval.
- Provide scoped supervisor access and incident handling.

## Non-Goals

- Payroll, recruitment, immigration advice, background checks, or host-organization HR systems.

## Decisions

### D0: Reuse existing platform boundaries

The module follows `OpenLearning.LearningPaths`' base-`DbContext` service and
configuration pattern, uses the existing Student/Admin authorization policies,
and stores learner evidence through `OpenLearning.Storage.StorageService` with
the private `Answer` purpose. External supervisors remain placement-scoped and
do not receive instructor or platform accounts.

### D1: Placement aggregate

`Placement` binds learner, program version, host, dates, coordinator, supervisor invitation, status, and competency plan. State transitions are explicit and audited.

### D2: Narrow supervisor principal

External supervisors receive expiring, revocable placement-scoped access and can view only assigned logs/evidence and evaluation forms.

### D3: Dual approval and amendments

Hour logs require learner submission plus supervisor approval. Corrections create amendments; they do not overwrite approved history.

### D4: Completion evaluator

Completion requires approved minimum hours, required competencies, evaluations, and resolved blocking incidents. Downstream credits/certificates consume the confirmed result idempotently.

## Risks / Trade-offs

- Evidence may contain workplace personal data; file rules, minimization, retention, and audited access are required.
- External identity recovery is higher risk; invitations and sessions are short-lived and revocable.

## Migration Plan

Add program, placement, supervisor, competency, log, evidence, evaluation, and incident tables. No existing assignment data is converted.
