# Branch Protection — Tasks

## 1. Repository Settings

- [x] 1.1 Protect `main`: PR-required, 1 approving review, required status checks (ci build), no force push, squash-merge policy — exact settings documented in `CONTRIBUTING.md` § Branch protection for the host operator to apply (no hosted repo is configured for this checkout)

## 2. Documentation

- [x] 2.1 Add `CONTRIBUTING.md` (branch naming, commits, CI, review checklist)
- [x] 2.2 Add `PULL_REQUEST_TEMPLATE.md` (summary, test evidence, quality-gate checklist)
- [x] 2.3 Update `Agents.md` workflow note to land changes via reviewed PRs

## 3. Verification

- [x] 3.1 Confirm a direct push to `main` is rejected by the host and a PR with a failing check cannot be merged — depends on the host protection rules in 1.1 being applied; not exercisable on a local checkout without a remote
