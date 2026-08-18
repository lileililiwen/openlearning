## Why

CI catches problems after a push, but contributors wait minutes for feedback. Local Git hooks catch format and build issues before commit/push, giving faster feedback. The quality plan's Phase 2 integrates Husky.Net for local hooks.

## What Changes

- Integrate Husky.Net into the solution for local Git hooks.
- Pre-commit hook: run `dotnet format --verify-no-changes` on staged files (fail commit on drift).
- Pre-push hook: run a fast build of the solution (fail push on build errors).
- Hooks are installed automatically on restore/init so new clones get them without manual steps.

## Capabilities

### New Capabilities
- `git-hooks`: local Husky.Net hooks enforcing format and build before commit/push.

### Modified Capabilities

- `lms-core`: local developer workflow gains hooks.

## Impact

- `Husky`/`HuskyTask` package reference in a root project (or tool manifest), `husky/` hook scripts (`pre-commit`, `pre-push`), and `HuskyTask` in `Directory.Build.props`.
- No CI change — CI remains the authoritative gate; hooks are a fast local pre-check.
