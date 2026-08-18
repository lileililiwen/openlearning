## Why

AI assistants generate a growing share of the code. The quality plan's Phase 3 asks for conventions for AI-generated code markers and PR review checklists so generated code gets extra scrutiny and its provenance is recorded.

## What Changes

- Convention: AI-generated or AI-substantially-assisted code is marked in the commit/PR description (e.g. `AI: generated`, `AI: assisted`, `AI: none` label or footer).
- PR review checklist codifies the extra review for AI-marked code: verify spec compliance, security review (authz, injection), tests present, no dead code.
- A lint/check (optionally) greps commit bodies for the marker and fails CI when the marker is missing on commits that touch many files (soft warning in MVP).

## Capabilities

### New Capabilities
- `ai-code-conventions`: AI-code provenance markers and AI-aware PR review checklist.

### Modified Capabilities

- `branch-protection`: the PR template gains AI markers and an AI-review checklist.
- `ci-pipeline`: optional marker lint step.

## Impact

- `PULL_REQUEST_TEMPLATE.md` gains an "AI involvement" selector and checklist.
- `CONTRIBUTING.md` documents marker values and review expectations.
- Optional CI lint (off by default) flags missing markers on large diffs.
