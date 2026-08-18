# Branch Protection — Tasks

## 1. Repository Settings

- [ ] 1.1 Protect `main`: PR-required, 1 approving review, required status checks (ci build), no force push, squash-merge policy

## 2. Documentation

- [ ] 2.1 Add `CONTRIBUTING.md` (branch naming, commits, CI, review checklist)
- [ ] 2.2 Add `PULL_REQUEST_TEMPLATE.md` (summary, test evidence, quality-gate checklist)
- [ ] 2.3 Update `Agents.md` workflow note to land changes via reviewed PRs

## 3. Verification

- [ ] 3.1 Confirm a direct push to `main` is rejected by the host and a PR with a failing check cannot be merged
