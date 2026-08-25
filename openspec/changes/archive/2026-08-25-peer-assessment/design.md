# Peer Assessment — Design

## Context

Assignments already model submissions and instructor grading. Peer assessment adds a second grading population (enrolled peers) whose input must not silently mutate academic records, mirroring how gamification stays separate from grades until an explicit boundary is crossed.

## Goals

- Workshop-style lifecycle: submission → review → closed, with allocation at review start.
- Deterministic, auditable allocation; rubric-structured assessments; policy-driven score combination.
- Anonymity and release controls that prevent early disclosure.

## Non-Goals

- Calibration training against instructor-scored samples (Open edX ORA2 style).
- Team/group assignments; reviewer grading of review quality.
- Cross-course or cross-org peer pools.

## Decisions

### D0: Reuse the existing module pattern and assignment entities

New `OpenLearning.PeerAssessment` module following §2: models, one `IEntityTypeConfiguration<T>` per entity, services on the base `DbContext`, `AddPeerAssessmentModule()`. The module references `OpenLearning.Assignments` to read submissions by id — it never writes assignment grade records. Final scores are written back only through the assignments module's existing grade path when the Instructor releases results with a strategy that includes instructor-recorded grades.

### D1: Allocation as a recorded, reproducible run

Allocation executes once when the review phase opens (or on explicit Instructor re-run), persisting an allocation run id per pair. A deterministic seeded round-robin over shuffled eligible students guarantees self-free distinct assignments and makes re-runs explainable. Shortfalls when the cohort is too small are stored on the run, not silently ignored.

### D2: Rubric locks at review-open; answers snapshot the rubric

Rubric questions are editable only before the review phase opens and are then locked. Each assessment answer stores a prompt and max-points snapshot, so later configuration changes cannot rewrite assessment history — matching the evidence-preservation intent of the badge-criteria versioning precedent in gamification without a full version tree.

### D3: Combination strategy computed server-side at release

Final score = f(instructor grade, mean of received peer assessments) under the configured strategy; manual override wins. Computation happens at release time from stored inputs so weight changes before release take effect, while released results keep the weights used.

### D4: Anonymity enforced at read time

Reviewer/reviewee identity filtering happens in query projections for student-facing pages, not by data deletion, so Instructors retain full visibility and audit integrity.

## Risks / Trade-offs

- Retaliatory or lazy reviews distort averages; anonymity plus instructor override mitigates without adding reviewer scoring yet.
- Small cohorts cannot satisfy review counts; explicit shortfall reporting avoids false completeness.
- Re-runs after late submissions change allocations; re-run requires explicit Instructor action and preserves prior assessments where pairs persist.

## Migration Plan

Add configuration, rubric question, allocation run, allocation pair, assessment, and result tables. No backfill: existing assignments simply have no peer review configuration until enabled.
