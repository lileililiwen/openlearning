## Why

Committers push directly to `main` with no review gate. The quality plan's Phase 1 requires branch protection: `main` is protected and changes land only through pull requests that pass CI.

## What Changes

- Protect `main`: require pull requests before merging, require a passing CI status check, and require review approval.
- Add a PR template and a contribution guide documenting the review flow and checklist.
- Enforce linear/mergeable history policy (documented; squash-merge option) and prevent force-push to `main`.

## Capabilities

### New Capabilities
- `branch-protection`: protected `main` with PR-only merging and required checks.

### Modified Capabilities

- `lms-core`: contribution workflow and repository settings documented.

## Impact

- Repository settings (host-level: GitHub branch protection rules).
- New `PULL_REQUEST_TEMPLATE.md` and `CONTRIBUTING.md` at the repo root.
- `Agents.md` workflow note updated: implementation commits land on feature branches reviewed via PR.

## Dependencies

- Requires `ci-pipeline` (the required status check must exist).
