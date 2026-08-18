# Git Hooks — Design

## Context

Husky.Net wires .NET commands into Git hooks without per-machine manual setup.

## Goals

- Hooks install automatically for every contributor.
- Commits are blocked on format drift; pushes are blocked on build failure.
- Hooks are fast enough not to annoy (format on staged files, build only on push).

## Non-Goals

- No hook running the full test suite locally (slow; CI owns tests).
- No policy enforcement — hooks are a convenience, CI is the gate.

## Decisions

### D1: Husky.Net integration
- Add `Husky` package (tool) via a local tool manifest (`dotnet new tool-manifest` + `dotnet tool install Husky`).
- `husky init` generates `husky/` scripts. `pre-commit` runs `dotnet format --verify-no-changes --include <staged>` and exits non-zero on drift; `pre-push` runs `dotnet build OpenLearning.sln --no-restore /warnaserror`.
- `HuskyTask` in `Directory.Build.props` installs hooks on restore (`Restore` target) so a fresh `dotnet restore` sets them up.

### D2: Scope of hooks
- Pre-commit format check is scoped to staged C#/cshtml files to stay fast; a full-tree check is optional via env var.
- Pre-push build uses the same warnings-as-errors setting as CI so local/remote agree.
- Hooks are shell scripts referencing `dotnet` on PATH; the `global.json` pins the SDK version (from `editorconfig-and-analyzers`).

## Risks / Trade-offs

- **Hook bypass** → `--no-verify` exists; hooks are best-effort, CI remains authoritative (documented in CONTRIBUTING).
- **Windows vs Linux** → Husky.Net generates cross-platform scripts; the repo is Linux-primary (documented).

## Migration Plan

No schema change. Tool manifest, `husky/` scripts, props task.

## Open Questions

- Should the pre-commit hook also run a quick analyzer build? MVP: no — pre-push build covers it.
