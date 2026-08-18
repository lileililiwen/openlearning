# EditorConfig & Analyzers — Tasks

## 1. Shared Build Configuration

- [ ] 1.1 Add root `.editorconfig` with C#, Razor, and file-layout rules
- [ ] 1.2 Add `Directory.Build.props` (Nullable, LangVersion, AnalysisLevel, TreatWarningsAsErrors, EnforceCodeStyleInBuild)
- [ ] 1.3 Add analyzer packages (NetAnalyzers via AnalysisLevel + SonarAnalyzer.CSharp) to the shared props
- [ ] 1.4 Remove per-project `Nullable`/`LangVersion` now inherited from props

## 2. Existing-Code Sweep

- [ ] 2.1 Run `dotnet build` and fix all analyzer violations (scoped pragma + justification only where truly needed)
- [ ] 2.2 Run `dotnet format` and commit the formatted tree
- [ ] 2.3 Verify the solution builds with 0 warnings / 0 errors under warnings-as-errors

## 3. Verification

- [ ] 3.1 `dotnet build OpenLearning.sln` is clean and fails on a deliberately introduced analyzer violation
