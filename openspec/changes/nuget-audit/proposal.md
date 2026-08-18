## Why

Dependencies are pinned but there is no automated vulnerability scanning. NuGet packages can ship CVEs; a compromised or outdated transitive dependency is a supply-chain risk. The quality plan's Phase 2 adds `dotnet-audit` for NuGet vulnerability scanning.

## What Changes

- Integrate `dotnet list package --vulnerable` (built-in) and/or the `dotnet-audit` CLI into CI.
- The audit step fails the pipeline on known high/critical vulnerabilities in direct and transitive packages.
- A policy documents the response: upgrade, pin, or explicitly accept with a justification (for non-fixable advisories).

## Capabilities

### New Capabilities
- `nuget-audit`: automated dependency vulnerability scanning in CI.

### Modified Capabilities

- `ci-pipeline`: adds the audit step to the workflow.

## Impact

- CI workflow gains an audit step (after restore, before/after build).
- `NuGetAudit`/`NuGetAuditMode` properties optionally set in `Directory.Build.props` to fail restore on known vulnerable packages.
- README/docs note the audit policy and the accepted-risk allowlist.
