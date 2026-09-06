## ADDED Requirements
### Requirement: Honest metrics
Published quality metrics SHALL reflect real scans or be explicitly marked not collected.
#### Scenario: Sonar did not run
- **WHEN** no real scan occurred
- **THEN** metrics are omitted or flagged 'not collected', not reported as zero
#### Scenario: Sonar ran
- **WHEN** a real scan occurred
- **THEN** the real values are published
