# CI Pipeline — Tasks

## 1. Workflow

- [x] 1.1 Add `.github/workflows/ci.yml` (push + PR on main, .NET 8, ubuntu-latest)
- [x] 1.2 Steps: checkout, setup-dotnet, restore, format verify, build /warnaserror, test
- [x] 1.3 Upload build logs on failure; README CI badge

## 2. Consistency

- [x] 2.1 Add `global.json` pinning the SDK (if not already present from the analyzer sweep)
- [x] 2.2 Verify the workflow passes on the current tree (format/build/test)

## 3. Verification

- [x] 3.1 Push a branch with a formatting violation and confirm CI fails; confirm the fixed branch passes
