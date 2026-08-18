# Git Hooks — Tasks

## 1. Husky.Net Integration

- [x] 1.1 Add local tool manifest + install `Husky` tool; run `husky init`
- [x] 1.2 Add `HuskyTask` to `Directory.Build.props` so hooks install on restore
- [x] 1.3 Pre-commit hook: `dotnet format --verify-no-changes` scoped to staged files
- [x] 1.4 Pre-push hook: `dotnet build OpenLearning.sln --no-restore /warnaserror`

## 2. Documentation

- [x] 2.1 Document hooks (and the `--no-verify` escape hatch) in `CONTRIBUTING.md`

## 3. Verification

- [x] 3.1 Commit a format violation → commit blocked; fix → allowed
- [x] 3.2 Introduce a build error → push blocked; fix → allowed
