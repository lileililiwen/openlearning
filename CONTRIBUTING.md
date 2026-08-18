# Contributing to OpenLearning

Thanks for contributing! This guide describes the workflow every change goes through — including documentation-only and spec-only changes.

## Branch protection on `main`

`main` is the integration branch and is **protected**. Apply these settings in the repository host (GitHub / CodeBuddy / GitLab, per your hosting):

- **Require a pull request before merging** — no direct pushes to `main`.
- **Require 1 approving review.**
- **Require status checks to pass before merging**, at minimum the `build` job of the `CI` workflow (see [`.github/workflows/ci.yml`](.github/workflows/ci.yml)). Sonar and package-audit checks join the required list as their gates land.
- **Do not allow force pushes.**
- **Squash merge preferred** to keep history linear; merge commits are also acceptable as a documented choice.

If you cannot edit repository settings yourself, ask a maintainer and point at this section.

## Getting started

- Read [`Agents.md`](Agents.md) — it is the normative contract for how changes are specified, implemented, and verified in this repository.
- The project follows **spec-first development**: every change starts as an OpenSpec change in `openspec/changes/` and is implemented, verified, archived, and committed one change at a time.

## Branches

- Create a **feature branch** for every change: `git checkout -b <change-name>` (e.g. `feat/course-tags`, or the OpenSpec change name).
- Keep the branch focused on one change. Do not stack unrelated work on a branch.
- Do not commit directly to `main` — the host rejects it.

## Commits

- Follow [Conventional Commits](https://www.conventionalcommits.org/) with a short imperative title:
  `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, `test:`.
- Title under ~72 characters; use the body for the *why* and *what*.
- Commit with your own identity; never overwrite history (`--force` push is blocked on `main` anyway).

## How CI verifies your change

The `CI` workflow runs on every push to `main` and every pull request:

1. `dotnet restore`
2. `dotnet format OpenLearning.sln --verify-no-changes` — fails on any formatting drift from `.editorconfig`
3. `dotnet build OpenLearning.sln -c Release /warnaserror` — fails on any compiler or analyzer warning
4. `dotnet test OpenLearning.sln -c Release --no-build` — fails on any failing test

The workflow **must pass before merge** (required status check).

## Local Git hooks

Husky.Net installs two local hooks automatically on the first `dotnet restore` (no manual setup):

- **pre-commit** — runs `dotnet format --verify-no-changes` on the staged C#/Razor files; the commit is blocked on formatting drift.
- **pre-push** — runs `dotnet build OpenLearning.sln --no-restore /warnaserror`; the push is blocked on build/analyzer errors.

Hooks are a **fast local pre-check** only — CI is the authoritative gate. They can be skipped for a single command with `--no-verify` (e.g. `git commit --no-verify`) or disabled entirely with `HUSKY=0`.

## Handling format / analyzer failures

- **Formatting drift** → run `dotnet format OpenLearning.sln`, review the diff, commit it. The SDK is pinned by [`global.json`](global.json); use a matching SDK so local and CI agree.
- **Analyzer warning (now an error)** → fix the code. If the rule genuinely cannot apply, use a *scoped* `#pragma warning disable <ID>` with a justification comment — never a blanket suppression. See the rationale in `openspec/specs/editorconfig-and-analyzers/spec.md`.
- **Test failure** → fix the code and re-run `dotnet test` before re-requesting review.

## Opening a pull request

- Use the [`PULL_REQUEST_TEMPLATE.md`](PULL_REQUEST_TEMPLATE.md) — it prompts for a summary, test evidence, and the quality-gate checklist.
- Tag the OpenSpec change(s) the PR implements (`openspec/changes/<name>` → `openspec/specs/<cap>/spec.md` after archiving).
- Wait for the required checks to pass and for one approving review before merging.
- After merge, the change must be **archived** (`openspec archive <name> -y`) and the archive commit lands through the same PR flow.

## Review checklist

For reviewers (and for the author before requesting review):

- [ ] OpenSpec change exists and all four artifacts (`proposal.md`, `spec.md`, `design.md`, `tasks.md`) are complete and validated
- [ ] Change is implemented one at a time, in roadmap order
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Build is clean (0 warnings / 0 errors under `/warnaserror`)
- [ ] Tests pass; scenarios in the spec are exercised (happy path **and** negative cases)
- [ ] No stubs, `TODO`s, or `NotImplementedException`
- [ ] Server-side validation and authorization on every mutating page; no secrets in logs/URLs
- [ ] No changes outside the change's scope (composition-root one-liners excepted)
- [ ] Commit message is conventional and explains *why*
