# Credit and Graduation — Design

## Context

Credits are durable academic records and cannot be recomputed solely from mutable course metadata.

## Goals

- Maintain an append-only, auditable credit ledger.
- Evaluate configurable program requirements deterministically.
- Separate eligibility calculation from an explicit graduation decision.

## Non-Goals

- Accreditation, transcripts signed by third parties, or certificate redesign.
- Learning-path sequencing.

## Decisions

### D1: Ledger-based awards

Persist `CreditAward` and compensating revocation entries with source, value, category, award time, and actor. Never overwrite awarded amounts.

### D2: Versioned program rules

Persist versioned `GraduationProgram` requirements. A learner is evaluated against their assigned version.

### D3: Explicit decision

The evaluator produces eligible/not-eligible with reasons. An authorized Admin records graduation only after a fresh evaluation.

## Risks / Trade-offs

- Retroactive course corrections require compensating entries.
- Complex requirement expressions are deferred in favor of totals, categories, and required-course sets.

## Migration Plan

Add credit/program tables. Existing completions receive no credits until an Admin runs an explicit backfill operation.
