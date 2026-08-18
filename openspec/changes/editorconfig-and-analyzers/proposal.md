## Why

The codebase has no enforced coding style or analyzer gates. Warnings are not treated as errors, style is inconsistent, and common defects (null dereferences, unused code, naming violations) ship unnoticed. The reference quality plan's Phase 1 places editorconfig and analyzers first because they are the fastest, highest-ROI protection.

## What Changes

- Add a root `.editorconfig` encoding style rules (indentation, naming, file layout) for C#, Razor, and dotnet-format.
- Add a `Directory.Build.props` that centralizes `Nullable=enable`, `LangVersion`, `AnalysisLevel`, `TreatWarningsAsErrors`, and `EnforceCodeStyleInBuild`.
- Add Roslyn analyzers (Microsoft.CodeAnalysis.NetAnalyzers) and `SonarAnalyzer.CSharp` to the shared build; analyzer warnings fail the build.
- Existing code is swept to satisfy the new rules (the build must stay at 0 warnings / 0 errors).

## Capabilities

### New Capabilities
- `editorconfig-and-analyzers`: enforced style rules and analyzer gates at build time.

### Modified Capabilities

- `lms-core`: the solution-wide build now enforces analyzers and warnings-as-errors.

## Impact

- New `.editorconfig` and `Directory.Build.props` at the repo root.
- All `src/*.csproj` drop per-project `Nullable`/`LangVersion` (inherited from the props) — small cleanup.
- CI and local `dotnet build` fail on analyzer violations; `dotnet format` enforces formatting.
