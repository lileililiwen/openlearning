# NuGet Audit — Design

## Context

NuGet ships vulnerability data with packages. .NET 8 restore can audit automatically, and `dotnet list package --vulnerable` reports on demand.

## Goals

- CI fails when packages have known vulnerabilities.
- The audit covers direct and transitive dependencies.
- Accepted-risk decisions are explicit and documented.

## Non-Goals

- No license compliance scanning (separate concern; not in scope).
- No private feed/registry integration beyond the configured NuGet sources.
- No automated dependency updates (a follow-up bot/PR flow is future work).

## Decisions

### D1: Restore-time audit
Set in `Directory.Build.props`:
- `<NuGetAudit>true</NuGetAudit>` (default in .NET 8, explicit)
- `<NuGetAuditMode>all</NuGetAuditMode>` (direct + transitive)
- `<NuGetAuditLevel>high</NuGetAuditLevel>` (fail on high/critical; moderate/low warn)

### D2: CI step
The workflow runs `dotnet list <sln> package --vulnerable --include-transitive` and greps for `Vulnerable`; any high/critical entry fails the pipeline. This provides a readable report beyond the restore-time error.

### D3: Accepted-risk policy
A `docs/security.md` (or `CONTRIBUTING` section) documents: upgrade → pin → accept. Accepted advisories are listed with rationale; `NuGetAuditSuppress` entries (package id + CVE) are the explicit mechanism and are reviewed in PRs.

## Risks / Trade-offs

- **False positives/blockers** → `NuGetAuditLevel` high-only plus an explicit suppress list avoids blocking on noisy moderate advisories.
- **Transitive exposure** → `--include-transitive` surfaces nested packages that direct-pin lists miss.

## Migration Plan

No schema change. Build props + CI step + policy doc.

## Open Questions

- Should `dotnet-outdated` update tooling be added? Deferred; audit is detection-only.
