# Branch Protection — Design

## Context

`main` is the integration branch and has no protection. Direct pushes bypass CI and review.

## Goals

- No direct pushes to `main`.
- Every change merges via a reviewed pull request that passes required checks.
- The process is documented so contributors follow it.

## Non-Goals

- No self-hosted git server config (applies to the hosted repo settings; documented, not code).
- No signed-commit requirement in MVP.
- No CODEOWNERS-based approval (single maintainer flow; optional later).

## Decisions

### D1: Host-level branch protection
Protect `main` with:
- Require a pull request before merging (0 direct pushes).
- Require 1 approving review.
- Require status checks to pass before merging: the `ci` workflow's `build` job (from `ci-pipeline`), and later Sonar and audit checks (Phase 2).
- Do not allow force pushes.
- Squash merge preferred (linear history), merge commits allowed as documented choice.

### D2: Repo documents
`CONTRIBUTING.md`: branch naming, commit message style (Conventional Commits), how CI runs, the review checklist, and how to handle analyzer/format failures. `PULL_REQUEST_TEMPLATE.md`: summary, test evidence (manual + automated), checklist of quality gates (format, build, tests, audit, sonar when present).

### D3: Agents.md alignment
Update the agent workflow checklist so spec-driven changes are implemented on a feature branch and merged via PR, instead of committing straight to `main` (the current in-repo contract). The existing archived-workflow note stays historical.

## Risks / Trade-offs

- **Process overhead** → PRs add a review step; the benefit (no broken `main`) outweighs it and matches the quality plan.
- **Single maintainer** → One approving review keeps momentum while still gating.

## Migration Plan

No schema change. Repository settings + two markdown docs + Agents.md note.

## Open Questions

- Should the rule apply to the `docs:`/spec-only commits too? Yes — PRs for everything keeps history reviewable.
