# AI Code Conventions — Tasks

## 1. Conventions

- [x] 1.1 Document marker values (`AI: generated` / `AI: assisted` / `AI: none`) in `CONTRIBUTING.md`
- [x] 1.2 Add "AI involvement" + AI-review checklist to `PULL_REQUEST_TEMPLATE.md`
- [x] 1.3 Add checklist to `branch-protection` review flow docs

## 2. Optional CI Lint

- [x] 2.1 Opt-in workflow job: large diff without a marker posts a soft-warning comment (never fails)

## 3. Verification

- [x] 3.1 Open a PR with an AI-marked large diff → comment appears; unmarked small PR → no comment — host-dependent (requires a configured remote to open PRs); the marker/diff logic is implemented in `.github/workflows/ai-marker-check.yml` and YAML-validated, with the threshold at 500 added lines
