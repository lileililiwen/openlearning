# Gamification — Design

## Context

Rewards can motivate participation but become unfair if client events can mint points or if leaderboards expose unwilling learners.

## Goals

- Award deterministic, auditable, capped points from trusted events.
- Issue reproducible badges and challenges.
- Make leaderboard participation optional and privacy-safe.

## Non-Goals

- Cash value, purchasable points, gambling mechanics, or effects on grades/credits.
- Punitive streak loss.

## Decisions

### D0: Reuse platform boundaries without reusing commerce loyalty balances

The module follows the existing base-`DbContext` module pattern and existing
Admin/Student policies, and uses course identifiers for scoped projections.
It deliberately does not write `OpenLearning.Ecommerce.PointsLedger`: that
ledger is payment-adjacent loyalty state, while gamification entries are
non-monetary, rule-versioned, source-idempotent, and independently auditable.

### D1: Append-only point ledger

Trusted server events create idempotent ledger entries using a rule version and source key. Corrections use compensating entries.

### D2: Versioned badge criteria

Badge awards record the exact criteria version and evidence; later rule changes do not revoke historical awards automatically.

### D3: Opt-in leaderboard

Users are excluded by default. Display aliases and rank bands where needed; tenant/course scope and moderation apply.

## Risks / Trade-offs

- Rewards can distort behavior; caps and operator disable controls are required.
- Ranking can expose performance; opt-in and minimal display reduce the risk.

## Migration Plan

Add rule, ledger, badge, challenge, preference, and projection tables. No retroactive awards unless explicitly previewed and run.
