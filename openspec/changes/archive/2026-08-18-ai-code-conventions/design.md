# AI Code Conventions — Design

## Context

AI-generated code can look plausible yet contain subtle logic, security, or licensing issues. Marking provenance routes it through a stricter review path.

## Goals

- Every PR records AI involvement explicitly.
- AI-marked code is reviewed against a specific checklist.
- The marker convention is lightweight (no tooling gate in MVP).

## Non-Goals

- No automated AI-detection (markers are self-declared).
- No blocking of AI-generated code.
- No runtime changes — conventions and docs only, plus an optional lint.

## Decisions

### D1: Marker values
Commit message footer or PR description includes one of:
- `AI: generated` — block written by an AI with minor human edits,
- `AI: assisted` — human-written with AI suggestions/refactors,
- `AI: none` — human-authored.

Commits omit the marker only when `AI: none` is declared at the PR level.

### D2: PR review checklist
`PULL_REQUEST_TEMPLATE.md` gains:
- An "AI involvement" select (generated/assisted/none).
- For generated/assisted: confirm spec compliance, ownership checks/authorization, injection-safe handling, tests or a stated reason for none, no dead code, and license/attribution of any copied fragments.

### D3: Optional CI lint
A workflow job (opt-in, non-required) scans the PR diff size and commit bodies: if the diff exceeds a threshold (e.g. 500 added lines) and no marker appears in the PR body/commits, it posts a comment (soft warning), never fails the pipeline.

## Risks / Trade-offs

- **Self-reporting** → Markers rely on honesty; the checklist adds accountability without tooling.
- **Marker noise** → Three values are simple; footer placement keeps commit messages readable.

## Migration Plan

No schema change. Template + contributing updates; optional workflow.

## Open Questions

- Should the marker be a required PR check? MVP: required in the template but not machine-enforced.
