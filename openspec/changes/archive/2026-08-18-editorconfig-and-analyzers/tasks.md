# EditorConfig & Analyzers — Tasks

## 1. Shared Build Configuration

- [x] 1.1 Add root `.editorconfig` with C#, Razor, and file-layout rules
- [x] 1.2 Add `Directory.Build.props` (Nullable, LangVersion, AnalysisLevel, TreatWarningsAsErrors, EnforceCodeStyleInBuild)
- [x] 1.3 Add analyzer packages (NetAnalyzers via AnalysisLevel + SonarAnalyzer.CSharp) to the shared props
- [x] 1.4 Remove per-project `Nullable`/`LangVersion` now inherited from props

## 2. Existing-Code Sweep

- [x] 2.1 Run `dotnet build` and fix all analyzer violations (scoped pragma + justification only where truly needed)
- [x] 2.2 Run `dotnet format` and commit the formatted tree
- [x] 2.3 Verify the solution builds with 0 warnings / 0 errors under warnings-as-errors

## 3. Verification

- [x] 3.1 `dotnet build OpenLearning.sln` is clean and fails on a deliberately introduced analyzer violation
