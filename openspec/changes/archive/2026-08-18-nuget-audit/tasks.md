# NuGet Audit — Tasks

## 1. Audit Configuration

- [x] 1.1 Set `NuGetAudit`, `NuGetAuditMode=all`, `NuGetAuditLevel=high` in `Directory.Build.props`
- [x] 1.2 Add CI audit step: `dotnet list ... package --vulnerable --include-transitive` failing on high/critical
- [x] 1.3 Add `docs/security.md` (or CONTRIBUTING section) documenting upgrade→pin→accept policy

## 2. Acceptance Mechanics

- [x] 2.1 Implement the `NuGetAuditSuppress` list for accepted advisories (reviewed in PRs) — property wired in `Directory.Build.props` (empty; no advisory accepted)

## 3. Verification

- [x] 3.1 Temporarily reference a package with a known high CVE → CI audit fails; remove → passes — restore fails with NU1903 (GHSA-qj66-m88j-hmgj) when injected, clean after removal; real transitive advisory (Microsoft.Extensions.Caching.Memory 8.0.0 via EF Core 8.0.8) fixed by upgrading EF Core to 8.0.30 / Npgsql provider to 8.0.11
