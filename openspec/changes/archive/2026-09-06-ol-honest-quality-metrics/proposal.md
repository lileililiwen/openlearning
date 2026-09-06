# Proposal: Stop emitting fake quality metrics
## Why
ci.yml 'Emit quality metrics' hardcodes bugs/vulnerabilities/codeSmells/duplication to 0 ({"bugs":0,...}) with a comment to fill real numbers later, so the published quality dashboard shows fake zeros and misleads reviewers.
## What Changes
- Gate the metrics on Sonar actually running; otherwise omit them or flag them as 'not collected'.
- Document the token requirement in CONTRIBUTING.

## Capabilities
### New Capabilities
- `ol-honest-quality-metrics`: the quality dashboard shows real scans or is explicitly marked not collected.

### Modified Capabilities
None.

## Impact
Affects: .github/workflows/ci.yml, CONTRIBUTING.md.
