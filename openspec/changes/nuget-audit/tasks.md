# NuGet Audit — Tasks

## 1. Audit Configuration

- [ ] 1.1 Set `NuGetAudit`, `NuGetAuditMode=all`, `NuGetAuditLevel=high` in `Directory.Build.props`
- [ ] 1.2 Add CI audit step: `dotnet list ... package --vulnerable --include-transitive` failing on high/critical
- [ ] 1.3 Add `docs/security.md` (or CONTRIBUTING section) documenting upgrade→pin→accept policy

## 2. Acceptance Mechanics

- [ ] 2.1 Implement the `NuGetAuditSuppress` list for accepted advisories (reviewed in PRs)

## 3. Verification

- [ ] 3.1 Temporarily reference a package with a known high CVE → CI audit fails; remove → passes
