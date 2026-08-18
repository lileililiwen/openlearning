# EditorConfig & Analyzers — Design

## Context

Each csproj repeats `<Nullable>enable</Nullable>`, there is no `.editorconfig`, and no analyzers run. The quality plan makes shared build properties and analyzers the first gate.

## Goals

- One source of truth for build properties and style.
- Analyzer violations fail the build (not just warnings).
- `dotnet format` output is deterministic.

## Non-Goals

- No third-party style tools (StyleCop) — Roslyn analyzers + Sonar cover it.
- No automatic code fixes in CI (format must be pre-applied by developers).
- No license-header enforcement.

## Decisions

### D1: `.editorconfig`
A root `.editorconfig` covering:
- indent size 4, tabs vs spaces,
- `dotnet_*` naming conventions (private `_camelCase`, public PascalCase),
- `csharp_*` and `dotnet_diagnostic.*.severity` rules,
- Razor/`.cshtml` file section with HTML/JS defaults.

### D2: `Directory.Build.props`
Central props at repo root (applies to all projects, including the Web project):
- `<Nullable>enable</Nullable>`, `<LangVersion>latest</LangVersion>`
- `<AnalysisLevel>latest-recommended</AnalysisLevel>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
- `<WarningsNotAsErrors>` for a small allowlist if a false-positive rule is unavoidable (kept minimal, documented).

### D3: Analyzer packages
Add to the props `PackageReference` (private assets) for:
- `Microsoft.CodeAnalysis.NetAnalyzers` (built-in .NET analyzers via AnalysisLevel)
- `SonarAnalyzer.CSharp` (latest stable)

Per-project csproj cleanup: remove redundant `<Nullable>`/`<LangVersion>` that now come from the props.

### D4: Existing-code sweep
A follow-on task sweeps the tree so the build is clean: fix warnings, suppress genuinely intentional diagnostics with scoped `#pragma` + justification comment (never blanket suppression).

## Risks / Trade-offs

- **Strictness friction** → `TreatWarningsAsErrors` can block unrelated PRs; mitigated by keeping the `WarningsNotAsErrors` allowlist tiny and documented.
- **Analyzer version drift** → Package versions pinned in the props; upgrade is a deliberate PR.

## Migration Plan

No schema change. Two new repo-root files + csproj cleanup.

## Open Questions

- Should formatting be enforced by `dotnet format --verify-no-changes` in CI or left to editorconfig-only? Phase 1: enforce in CI (`ci-pipeline` change).
